using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Interfaces.AssetHistories;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.AssetHistories;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.ServiceCases.DisposeServiceCaseAsset;

/// <summary>
/// Handles asset disposal through an active service case.
/// </summary>
/// <remarks>
/// Only active application administrators can perform this operation.
///
/// The asset is marked as disposed and the service case is resolved.
/// Closing the service case remains a separate operation.
///
/// An asset located at an external service provider may remain there
/// for donor parts without being returned to headquarters.
///
/// The disposal reason is recorded in the service case event
/// and in the asset history.
///
/// All repositories must share the same scoped persistence context
/// so that the asset, service case, event, and history entries
/// are persisted together in a single save.
/// </remarks>
public sealed class DisposeServiceCaseAssetHandler
{
    private const int MaximumReasonLength = 2000;
    private const string DisposalReasonPropertyName = "DisposalReason";

    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IAssetHistoryRepository _assetHistoryRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IPackageRepository _packageRepository;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="DisposeServiceCaseAssetHandler"/> class.
    /// </summary>
    /// <param name="serviceCaseRepository">
    /// The repository used to load the service case,
    /// record its disposal event, and save the changes.
    /// </param>
    /// <param name="assetRepository">
    /// The repository used to load the asset for modification.
    /// </param>
    /// <param name="assetHistoryRepository">
    /// The repository used to record the asset disposal history.
    /// </param>
    /// <param name="currentApplicationUserResolver">
    /// The resolver used to identify the performing application user.
    /// </param>
    /// <param name="packageRepository">
    /// The repository used to verify that the asset is not part of a package.
    /// </param>
    /// <param name="assetOperationExecutor">
    /// The executor used to perform the asset disposal operation within a transaction.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// A required dependency is null.
    /// </exception>
    public DisposeServiceCaseAssetHandler(
        IServiceCaseRepository serviceCaseRepository,
        IAssetRepository assetRepository,
        IAssetHistoryRepository assetHistoryRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IPackageRepository packageRepository,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(assetHistoryRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _serviceCaseRepository = serviceCaseRepository;
        _assetRepository = assetRepository;
        _assetHistoryRepository = assetHistoryRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _packageRepository = packageRepository;
        _assetOperationExecutor = assetOperationExecutor;
    }

    /// <summary>
    /// Disposes of the asset associated with a service case within
    /// a transaction protected by an exclusive asset-level lock.
    /// </summary>
    /// <remarks>
    /// A preliminary no-tracking lookup identifies the asset to lock.
    /// The service case is loaded again inside the protected operation,
    /// and its asset relationship is revalidated before disposal.
    ///
    /// Authorization, active package validation, state changes,
    /// audit creation, and saving occur inside the protected operation.
    /// </remarks>
    /// <param name="serviceCaseId">
    /// The identifier of the service case.
    /// </param>
    /// <param name="request">
    /// The request containing the mandatory disposal reason.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lookup, lock acquisition, and execution.
    /// </param>
    /// <returns>
    /// The disposal result after the transaction has been committed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the service case identifier or disposal reason is invalid.
    /// </exception>
    /// <exception cref="ServiceCaseNotFoundException">
    /// Thrown when the service case cannot be found.
    /// </exception>
    /// <exception cref="ServiceCaseAccessDeniedException">
    /// Thrown when the current user cannot perform disposal.
    /// </exception>
    /// <exception cref="AssetNotFoundException">
    /// Thrown when the associated asset cannot be found.
    /// </exception>
    /// <exception cref="ServiceCaseStateConflictException">
    /// Thrown when the asset relationship has changed, the asset belongs
    /// to an active package, or the current state does not allow disposal.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the asset lock cannot be acquired within the timeout.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the execution context does not support
    /// the protected operation.
    /// </exception>
    public async Task<DisposeServiceCaseAssetResponse> HandleAsync(
        Guid serviceCaseId,
        DisposeServiceCaseAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);

        _ = ValidateAndNormalizeReason(request.Reason);

        // This lookup must remain no-tracking.
        var serviceCaseSnapshot =
            await _serviceCaseRepository.GetByIdAsync(
                serviceCaseId,
                cancellationToken)
            ?? throw new ServiceCaseNotFoundException(serviceCaseId);

        var assetId = serviceCaseSnapshot.AssetId;

        if (assetId == Guid.Empty)
        {
            throw new ServiceCaseStateConflictException(
                serviceCaseId,
                "The service case does not reference a valid asset.");
        }

        return await _assetOperationExecutor
            .ExecuteAsync<DisposeServiceCaseAssetResponse>(
                assetIds: new[] { assetId },
                operation: token => HandleCoreAsync(
                    serviceCaseId,
                    assetId,
                    request,
                    token),
                cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Disposes of the asset associated with the service case
    /// and resolves the service case.
    /// </summary>
    /// <param name="serviceCaseId">
    /// The unique identifier of the service case.
    /// </param>
    /// <param name="request">
    /// The request containing the mandatory disposal reason.
    /// </param>
    /// <param name="lockedAssetId">
    /// The unique identifier of the asset that has been locked
    /// for exclusive access during the operation.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The updated service case and asset statuses,
    /// disposal reason, timestamp, and performing administrator.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The service case identifier is empty, or the disposal reason
    /// is empty or exceeds 2000 characters after trimming.
    /// </exception>
    /// <exception cref="ServiceCaseAccessDeniedException">
    /// The current user is inactive or is not an application administrator.
    /// </exception>
    /// <exception cref="ServiceCaseNotFoundException">
    /// The requested service case does not exist.
    /// </exception>
    /// <exception cref="AssetNotFoundException">
    /// The asset associated with the service case does not exist.
    /// </exception>
    /// <exception cref="ServiceCaseStateConflictException">
    /// The service case or asset state does not allow disposal,
    /// their relationship is inconsistent, or the asset
    /// is still assigned to a user.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled.
    /// </exception>
    public async Task<DisposeServiceCaseAssetResponse> HandleCoreAsync(
        Guid serviceCaseId,
        Guid lockedAssetId,
        DisposeServiceCaseAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);

        var reason = ValidateAndNormalizeReason(request.Reason);

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

        EnsureCanDispose(currentUser);

        var serviceCase =
    await _serviceCaseRepository.GetForUpdateByIdAsync(
        serviceCaseId,
        cancellationToken)
    ?? throw new ServiceCaseNotFoundException(serviceCaseId);

    if (serviceCase.AssetId != lockedAssetId)
    {
        throw new ServiceCaseStateConflictException(
            serviceCaseId,
            "The asset associated with the service case changed " +
            "before the operation acquired its lock. " +
            "Reload the service case and try again.");
    }

    var asset =
        await _assetRepository.GetForUpdateByIdAsync(
            lockedAssetId,
            cancellationToken)
        ?? throw new AssetNotFoundException(lockedAssetId);

        var hasActivePackage = await _packageRepository.HasActivePackageForAssetAsync(asset.Id, cancellationToken);

        if (hasActivePackage)
        {
            throw new ServiceCaseStateConflictException(
                serviceCase.Id,
                "An asset included in an active package cannot be disposed of. " +
                "Remove it from the package or cancel the package before dispatch. " +
                "For a package in transit, receive the package first.");
        }

ValidateConsistentState(serviceCase, asset);

        var previousServiceCaseStatus = serviceCase.Status;
        var previousAssetStatus = asset.AssetStatus;

        try
        {
            serviceCase.ResolveByAssetDisposal();
            asset.Dispose();
        }
        catch (InvalidOperationException exception)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, exception.Message);
        }

        var disposedAtUtc = DateTime.UtcNow;

        var serviceCaseEvent = ServiceCaseEvent.Create(
            serviceCaseId: serviceCase.Id,
            type: ServiceCaseEventType.AssetDisposed,
            previousStatus: previousServiceCaseStatus,
            newStatus: serviceCase.Status,
            comment: reason,
            performedByUserId: currentUser.Id,
            performedByUserName: currentUser.DisplayName,
            occurredAtUtc: disposedAtUtc);

        var histories = CreateHistories(
            asset,
            previousAssetStatus,
            reason,
            currentUser,
            disposedAtUtc);

        await _serviceCaseRepository.AddEventAsync(serviceCaseEvent, cancellationToken);

        await _assetHistoryRepository.AddRangeAsync(histories, cancellationToken);

        // Persist all tracked changes in a single save.
        await _serviceCaseRepository.SaveChangesAsync(cancellationToken);

        return new DisposeServiceCaseAssetResponse(
            ServiceCaseId: serviceCase.Id,
            CaseNumber: serviceCase.CaseNumber,
            AssetId: asset.Id,
            ServiceCaseStatus: serviceCase.Status,
            AssetStatus: asset.AssetStatus,
            Reason: reason,
            DisposedAtUtc: disposedAtUtc,
            DisposedByUserId: currentUser.Id,
            DisposedByUserName: currentUser.DisplayName);
    }

    /// <summary>
    /// Validates the service case identifier.
    /// </summary>
    /// <param name="serviceCaseId">
    /// The service case identifier to validate.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The identifier is empty.
    /// </exception>
    private static void ValidateServiceCaseId(Guid serviceCaseId)
    {
        if (serviceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
        }
    }

    /// <summary>
    /// Validates and trims the mandatory disposal reason.
    /// </summary>
    /// <param name="reason">
    /// The disposal reason supplied by the client.
    /// </param>
    /// <returns>
    /// The validated and trimmed disposal reason.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The reason is empty or exceeds 2000 characters after trimming.
    /// </exception>
    private static string ValidateAndNormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A disposal reason is required.", nameof(reason));
        }

        var normalizedReason = reason.Trim();

        if (normalizedReason.Length > MaximumReasonLength)
        {
            throw new ArgumentException($"The disposal reason cannot exceed {MaximumReasonLength} characters.", nameof(reason));
        }

        return normalizedReason;
    }

    /// <summary>
    /// Ensures that the performing user is an active
    /// application administrator.
    /// </summary>
    /// <param name="currentUser">
    /// The application user performing the operation.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The application user is null.
    /// </exception>
    /// <exception cref="ServiceCaseAccessDeniedException">
    /// The user is inactive or is not an application administrator.
    /// </exception>
    private static void EnsureCanDispose(ApplicationUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        if (!currentUser.IsActive || currentUser.Role != ApplicationRole.ApplicationAdministrator)
        {
            throw new ServiceCaseAccessDeniedException("Only active application administrators can dispose of assets through a service case.");
        }
    }

    /// <summary>
    /// Validates the relationship, source states, origin store,
    /// and user assignments before disposal.
    /// </summary>
    /// <param name="serviceCase">
    /// The service case associated with the asset.
    /// </param>
    /// <param name="asset">
    /// The asset to dispose of.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The service case or asset is null.
    /// </exception>
    /// <exception cref="ServiceCaseStateConflictException">
    /// The relationship, source states, origin store,
    /// or user assignments do not allow disposal.
    /// </exception>
    private static void ValidateConsistentState(ServiceCase serviceCase, Asset asset)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);
        ArgumentNullException.ThrowIfNull(asset);

        if (serviceCase.AssetId != asset.Id)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "The service case and asset relationship is invalid.");
        }

        if (!serviceCase.IsActive)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "Only an active service case can be used for asset disposal.");
        }

        var expectedAssetStatus = serviceCase.Status switch
        {
            ServiceCaseStatus.UnderCentralInspection => AssetStatus.In_HQ,
            ServiceCaseStatus.AwaitingExternalServiceDispatch => AssetStatus.In_HQ,
            ServiceCaseStatus.InExternalService => AssetStatus.In_Service,
            ServiceCaseStatus.AwaitingReturn => AssetStatus.In_HQ,

            _ => throw new ServiceCaseStateConflictException(serviceCase.Id, "The current service case status does not allow asset disposal.")
        };

        if (asset.AssetStatus != expectedAssetStatus)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "The asset status is inconsistent with the service case status for disposal.");
        }

        if (serviceCase.OriginStoreId.HasValue && (serviceCase.OriginStoreId.Value == Guid.Empty || asset.AssignedStoreId != serviceCase.OriginStoreId.Value))
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "The asset is not assigned to a valid service case origin store.");
        }

        if (asset.AssignedEmployeeNumber is not null || asset.AssignedUserId.HasValue)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "The asset must be returned from employee use and unassigned from its application user before disposal.");
        }
    }

    /// <summary>
    /// Creates the asset status change and disposal reason audit entries.
    /// </summary>
    /// <param name="asset">
    /// The asset after disposal.
    /// </param>
    /// <param name="previousStatus">
    /// The asset status before disposal.
    /// </param>
    /// <param name="reason">
    /// The validated disposal reason.
    /// </param>
    /// <param name="performedBy">
    /// The administrator who performed the disposal.
    /// </param>
    /// <param name="disposedAtUtc">
    /// The UTC timestamp shared by the event and history entries.
    /// </param>
    /// <returns>
    /// The asset status change entry and disposal reason entry.
    /// </returns>
    /// <remarks>
    /// DisposalReason is an audit metadata key, not an Asset property.
    /// The audit format matches standalone asset disposal.
    /// </remarks>
    private static IReadOnlyList<AssetHistory> CreateHistories(
        Asset asset,
        AssetStatus previousStatus,
        string reason,
        ApplicationUser performedBy,
        DateTime disposedAtUtc)
    {
        return
        [
            new AssetHistory(
                assetId: asset.Id,
                action: nameof(Asset.Dispose),
                propertyName: nameof(Asset.AssetStatus),
                oldValue: previousStatus.ToString(),
                newValue: asset.AssetStatus.ToString(),
                changedByUserId: performedBy.Id,
                changedByUserName: performedBy.DisplayName,
                changedAtUtc: disposedAtUtc),

            new AssetHistory(
                assetId: asset.Id,
                action: nameof(Asset.Dispose),
                propertyName: DisposalReasonPropertyName,
                oldValue: null,
                newValue: reason,
                changedByUserId: performedBy.Id,
                changedByUserName: performedBy.DisplayName,
                changedAtUtc: disposedAtUtc)
        ];
    }
}