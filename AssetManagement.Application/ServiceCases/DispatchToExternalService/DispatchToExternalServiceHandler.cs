using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.AssetHistories;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.AssetHistories;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.ServiceCases.DispatchToExternalService;

public sealed class DispatchToExternalServiceHandler
{
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IAssetHistoryRepository _assetHistoryRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    public DispatchToExternalServiceHandler(
        IServiceCaseRepository serviceCaseRepository,
        IAssetRepository assetRepository,
        IAssetHistoryRepository assetHistoryRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(assetHistoryRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _serviceCaseRepository = serviceCaseRepository;
        _assetRepository = assetRepository;
        _assetHistoryRepository = assetHistoryRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _assetOperationExecutor = assetOperationExecutor;
    }

    /// <summary>
    /// Dispatches an asset to external service within a transaction
    /// protected by an exclusive asset-level lock.
    /// </summary>
    /// <remarks>
    /// A preliminary no-tracking lookup identifies the asset to lock.
    /// The service case and asset are reloaded after acquiring the lock.
    ///
    /// Authorization, state validation, state changes, audit creation,
    /// and persistence occur inside the protected transaction.
    /// </remarks>
    /// <param name="serviceCaseId">
    /// The identifier of the service case.
    /// </param>
    /// <param name="request">
    /// The external service dispatch request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lookup, lock acquisition, and execution.
    /// </param>
    /// <returns>
    /// The dispatch result after the transaction commits.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the service case identifier is empty.
    /// </exception>
    /// <exception cref="ServiceCaseNotFoundException">
    /// Thrown when the service case cannot be found.
    /// </exception>
    /// <exception cref="ServiceCaseStateConflictException">
    /// Thrown when the service case relationship or state is invalid.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the asset lock cannot be acquired within the timeout.
    /// </exception>
    public async Task<DispatchToExternalServiceResponse> HandleAsync(
        Guid serviceCaseId,
        DispatchToExternalServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);

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
            .ExecuteAsync<DispatchToExternalServiceResponse>(
                assetIds: new[] { assetId },
                operation: token => HandleCoreAsync(
                    serviceCaseId,
                    assetId,
                    request,
                    token),
                cancellationToken: cancellationToken);
    }

    private async Task<DispatchToExternalServiceResponse> HandleCoreAsync(
        Guid serviceCaseId,
        Guid lockedAssetId, 
        DispatchToExternalServiceRequest request, 
        CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

        ServiceCaseAuthorization.EnsureCanManageAll(currentUser);

        var serviceCase =
            await _serviceCaseRepository.GetForUpdateByIdAsync(
                serviceCaseId,
                cancellationToken)
            ?? throw new ServiceCaseNotFoundException(serviceCaseId);

        var asset =
            await _assetRepository.GetForUpdateByIdAsync(
                serviceCase.AssetId,
                cancellationToken)
            ?? throw new AssetNotFoundException(serviceCase.AssetId);

        ValidateConsistentState(serviceCase, asset);

        var previousServiceCaseStatus = serviceCase.Status;
        var previousAssetStatus = asset.AssetStatus;
        var dispatchedAtUtc = DateTime.UtcNow;

        serviceCase.DispatchToExternalService();
        asset.DispatchToExternalService();

        var serviceCaseEvent = ServiceCaseEvent.Create(
            serviceCaseId: serviceCase.Id,
            type: ServiceCaseEventType.DispatchedToExternalService,
            previousStatus: previousServiceCaseStatus,
            newStatus: serviceCase.Status,
            comment: request.Comment,
            performedByUserId: currentUser.Id,
            performedByUserName: currentUser.DisplayName,
            occurredAtUtc: dispatchedAtUtc);

        var assetHistory = new AssetHistory(
            assetId: asset.Id,
            action: nameof(Asset.DispatchToExternalService),
            propertyName: nameof(Asset.AssetStatus),
            oldValue: previousAssetStatus.ToString(),
            newValue: asset.AssetStatus.ToString(),
            changedByUserId: currentUser.Id,
            changedByUserName: currentUser.DisplayName,
            changedAtUtc: dispatchedAtUtc);

        await _serviceCaseRepository.AddEventAsync(serviceCaseEvent, cancellationToken);
        await _assetHistoryRepository.AddRangeAsync([assetHistory], cancellationToken);
        await _serviceCaseRepository.SaveChangesAsync(cancellationToken);

        return MapResponse(serviceCase, asset, currentUser, dispatchedAtUtc);
    }

    private static DispatchToExternalServiceResponse MapResponse(
        ServiceCase serviceCase,
        Asset asset,
        ApplicationUser currentUser,
        DateTime dispatchedAtUtc)
    {
        if (!serviceCase.VendorId.HasValue || serviceCase.VendorId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("The service case does not have a valid assigned vendor.");
        }

        return new DispatchToExternalServiceResponse(
            ServiceCaseId: serviceCase.Id,
            CaseNumber: serviceCase.CaseNumber,
            AssetId: asset.Id,
            ServiceCaseStatus: serviceCase.Status,
            AssetStatus: asset.AssetStatus,
            VendorId: serviceCase.VendorId.Value,
            DispatchedAtUtc: dispatchedAtUtc,
            DispatchedByUserId: currentUser.Id,
            DispatchedByUserName: currentUser.DisplayName);
    }

    private static void ValidateServiceCaseId(Guid serviceCaseId)
    {
        if (serviceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
        }
    }

    private static void ValidateConsistentState(ServiceCase serviceCase, Asset asset)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);
        ArgumentNullException.ThrowIfNull(asset);

        if (serviceCase.AssetId != asset.Id)
        {
            throw new InvalidOperationException("The service case and asset relationship is invalid.");
        }

        if (serviceCase.Status != ServiceCaseStatus.AwaitingExternalServiceDispatch)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "Only a service case awaiting external service dispatch can be sent to external service.");
        }

        if (!serviceCase.VendorId.HasValue || serviceCase.VendorId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("A vendor must be assigned before the service case can be sent to external service.");
        }

        if (asset.AssetStatus != AssetStatus.In_HQ)
        {
            throw new InvalidOperationException("Only an asset located at headquarters can be sent to external service.");
        }
    }
}