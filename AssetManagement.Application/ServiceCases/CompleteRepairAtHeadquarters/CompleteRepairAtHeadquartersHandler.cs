using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.ServiceCases.CompleteRepairAtHeadquarters;

public sealed class CompleteRepairAtHeadquartersHandler
{
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    public CompleteRepairAtHeadquartersHandler(
        IServiceCaseRepository serviceCaseRepository,
        IAssetRepository assetRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _serviceCaseRepository = serviceCaseRepository;
        _assetRepository = assetRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _assetOperationExecutor = assetOperationExecutor;
    }

    /// <summary>
    /// Completes a repair at headquarters within a transaction protected
    /// by an exclusive lock for the associated asset.
    /// </summary>
    /// <remarks>
    /// A preliminary no-tracking lookup identifies the asset to lock.
    ///
    /// The service case is reloaded inside the protected operation.
    /// Authorization, business validation, state changes, event creation,
    /// and persistence are performed after acquiring the asset lock.
    /// </remarks>
    /// <param name="serviceCaseId">
    /// The identifier of the service case.
    /// </param>
    /// <param name="request">
    /// The repair completion request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lookup, lock acquisition, and execution.
    /// </param>
    /// <returns>
    /// The completed repair information after the transaction commits.
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
    public async Task<CompleteRepairAtHeadquartersResponse> HandleAsync(
        Guid serviceCaseId,
        CompleteRepairAtHeadquartersRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);

        // This repository method must remain no-tracking.
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
            .ExecuteAsync<CompleteRepairAtHeadquartersResponse>(
                assetIds: new[] { assetId },
                operation: token => HandleCoreAsync(
                    serviceCaseId,
                    assetId,
                    request,
                    token),
                cancellationToken: cancellationToken);
    }

    private async Task<CompleteRepairAtHeadquartersResponse> HandleCoreAsync(
        Guid serviceCaseId,
        Guid lockedAssetId,
        CompleteRepairAtHeadquartersRequest request,
        CancellationToken cancellationToken)
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
                "The asset associated with the service case changed " +
                "before the operation acquired its lock.");
        }

        var asset =
            await _assetRepository.GetByIdAsync(
                lockedAssetId,
                cancellationToken)
            ?? throw new AssetNotFoundException(lockedAssetId);

        ValidateConsistentState(serviceCase, asset);

        var previousServiceCaseStatus = serviceCase.Status;
        var completedAtUtc = DateTime.UtcNow;

        serviceCase.CompleteRepairAtHeadquarters();

        var serviceCaseEvent = ServiceCaseEvent.Create(
            serviceCaseId: serviceCase.Id,
            type: ServiceCaseEventType.RepairCompletedInHeadquarters,
            previousStatus: previousServiceCaseStatus,
            newStatus: serviceCase.Status,
            comment: request.Comment,
            performedByUserId: currentUser.Id,
            performedByUserName: currentUser.DisplayName,
            occurredAtUtc: completedAtUtc);

        await _serviceCaseRepository.AddEventAsync(serviceCaseEvent, cancellationToken);
        await _serviceCaseRepository.SaveChangesAsync(cancellationToken);
        return CreateResponse(serviceCase, asset, currentUser, completedAtUtc);
    }

    private static CompleteRepairAtHeadquartersResponse CreateResponse(
        ServiceCase serviceCase,
        Asset asset,
        ApplicationUser currentUser,
        DateTime completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(currentUser);

        return new CompleteRepairAtHeadquartersResponse(
            ServiceCaseId: serviceCase.Id,
            CaseNumber: serviceCase.CaseNumber,
            AssetId: asset.Id,
            ServiceCaseStatus: serviceCase.Status,
            AssetStatus: asset.AssetStatus,
            CompletedAtUtc: completedAtUtc,
            CompletedByUserId: currentUser.Id,
            CompletedByUserName: currentUser.DisplayName);
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
            throw new ServiceCaseStateConflictException(serviceCase.Id, "The service case and asset relationship is invalid.");
        }

        if (serviceCase.Status != ServiceCaseStatus.UnderCentralInspection)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "Only a service case under central inspection can have its repair completed at headquarters.");
        }

        if (asset.AssetStatus != AssetStatus.In_HQ)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "Only an asset located at headquarters can have its repair completed at headquarters.");
        }

        if (serviceCase.OriginStoreId.HasValue)
        {
            if (serviceCase.OriginStoreId.Value == Guid.Empty)
            {
                throw new ServiceCaseStateConflictException(serviceCase.Id, "The origin store identifier cannot be empty.");
            }

            if (asset.AssignedStoreId != serviceCase.OriginStoreId.Value)
            {
                throw new ServiceCaseStateConflictException(serviceCase.Id, "The asset is not assigned to the service case origin store.");
            }
        }
    }
}