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

namespace AssetManagement.Application.ServiceCases.ConfirmReceivedByExternalService;

public sealed class ConfirmReceivedByExternalServiceHandler
{
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IAssetHistoryRepository _assetHistoryRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    public ConfirmReceivedByExternalServiceHandler(
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
    /// Confirms receipt by external service within a transaction
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
    /// The external service receipt confirmation request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lookup, lock acquisition, and execution.
    /// </param>
    /// <returns>
    /// The confirmation result after the transaction commits.
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
    public async Task<ConfirmReceivedByExternalServiceResponse> HandleAsync(
        Guid serviceCaseId,
        ConfirmReceivedByExternalServiceRequest request,
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
            .ExecuteAsync<ConfirmReceivedByExternalServiceResponse>(
                assetIds: new[] { assetId },
                operation: token => HandleCoreAsync(
                    serviceCaseId,
                    assetId,
                    request,
                    token),
                cancellationToken: cancellationToken);
    }

    private async Task<ConfirmReceivedByExternalServiceResponse>HandleCoreAsync(
            Guid serviceCaseId,
            Guid lockedAssetId,
            ConfirmReceivedByExternalServiceRequest request,
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

        if (serviceCase.AssetId != lockedAssetId)
        {
            throw new ServiceCaseStateConflictException(
                serviceCaseId,
                "The asset associated with the service case changed before the operation acquired its lock.");
        }

        var asset =
            await _assetRepository.GetForUpdateByIdAsync(
                lockedAssetId,
                cancellationToken)
            ?? throw new AssetNotFoundException(lockedAssetId);

        ValidateConsistentState(serviceCase, asset);

        var previousServiceCaseStatus = serviceCase.Status;
        var previousAssetStatus = asset.AssetStatus;
        var confirmedAtUtc = DateTime.UtcNow;

        serviceCase.ConfirmReceivedByExternalService();

        asset.ConfirmReceivedByExternalService();

        var serviceCaseEvent = ServiceCaseEvent.Create(
                serviceCaseId: serviceCase.Id,
                type: ServiceCaseEventType.ReceivedByExternalService,
                previousStatus: previousServiceCaseStatus,
                newStatus: serviceCase.Status,
                comment: request.Comment,
                performedByUserId: currentUser.Id,
                performedByUserName: currentUser.DisplayName,
                occurredAtUtc: confirmedAtUtc);

        var assetHistory = new AssetHistory(
                assetId: asset.Id,
                action: nameof(Asset.ConfirmReceivedByExternalService),
                propertyName: nameof(Asset.AssetStatus),
                oldValue: previousAssetStatus.ToString(),
                newValue: asset.AssetStatus.ToString(),
                changedByUserId: currentUser.Id,
                changedByUserName: currentUser.DisplayName,
                changedAtUtc: confirmedAtUtc);

        await _serviceCaseRepository.AddEventAsync(serviceCaseEvent, cancellationToken);
        await _assetHistoryRepository.AddRangeAsync([assetHistory], cancellationToken);
        await _serviceCaseRepository.SaveChangesAsync(cancellationToken);

        return CreateResponse(
            serviceCase,
            asset,
            currentUser,
            confirmedAtUtc);
    }

    private static ConfirmReceivedByExternalServiceResponse
        CreateResponse(
            ServiceCase serviceCase,
            Asset asset,
            ApplicationUser currentUser,
            DateTime confirmedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(currentUser);

        if (!serviceCase.VendorId.HasValue || serviceCase.VendorId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("The service case does not have a valid assigned vendor.");
        }

        return new ConfirmReceivedByExternalServiceResponse(
            ServiceCaseId: serviceCase.Id,
            CaseNumber: serviceCase.CaseNumber,
            AssetId: asset.Id,
            ServiceCaseStatus: serviceCase.Status,
            AssetStatus: asset.AssetStatus,
            VendorId: serviceCase.VendorId.Value,
            ConfirmedAtUtc: confirmedAtUtc,
            ConfirmedByUserId: currentUser.Id,
            ConfirmedByUserName: currentUser.DisplayName);
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

        if (serviceCase.Status != ServiceCaseStatus.SentToExternalService)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "Only a service case sent to external service can be marked as received by the external service.");
        }

        if (!serviceCase.VendorId.HasValue || serviceCase.VendorId.Value == Guid.Empty)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "A valid vendor must be assigned before external service receipt can be confirmed.");
        }

        if (asset.AssetStatus != AssetStatus.Route_To_Service)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "Only an asset being routed to external service can be marked as received by the external service.");
        }
    }
}