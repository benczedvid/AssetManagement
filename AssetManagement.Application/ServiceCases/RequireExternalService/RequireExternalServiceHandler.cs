using AssetManagement.Application.Assets;
using AssetManagement.Application.Vendors;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Common.Interfaces.Vendors;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Domain.Entities.Vendors;
using AssetManagement.Application.Common.Authorization;

namespace AssetManagement.Application.ServiceCases.RequireExternalService;

public sealed class RequireExternalServiceHandler
{
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    public RequireExternalServiceHandler(
        IServiceCaseRepository serviceCaseRepository,
        IAssetRepository assetRepository,
        IVendorRepository vendorRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(vendorRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _serviceCaseRepository = serviceCaseRepository;
        _assetRepository = assetRepository;
        _vendorRepository = vendorRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _assetOperationExecutor = assetOperationExecutor;
    }

    /// <summary>
    /// Assigns an external service vendor within a transaction protected
    /// by an exclusive lock for the associated asset.
    /// </summary>
    /// <remarks>
    /// A preliminary no-tracking lookup identifies the asset to lock.
    /// The service case is reloaded inside the protected operation.
    ///
    /// Authorization, vendor validation, state changes, event creation,
    /// and persistence occur after acquiring the asset lock.
    /// </remarks>
    /// <param name="serviceCaseId">
    /// The identifier of the service case.
    /// </param>
    /// <param name="request">
    /// The external service assignment request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lookup, lock acquisition, and execution.
    /// </param>
    /// <returns>
    /// The updated service case information after the transaction commits.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the service case or vendor identifier is invalid.
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
    public async Task<RequireExternalServiceResponse> HandleAsync(
        Guid serviceCaseId,
        RequireExternalServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateVendorId(request.VendorId);

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
            .ExecuteAsync<RequireExternalServiceResponse>(
                assetIds: new[] { assetId },
                operation: token => HandleCoreAsync(
                    serviceCaseId,
                    assetId,
                    request,
                    token),
                cancellationToken: cancellationToken);
    }

    private async Task<RequireExternalServiceResponse>HandleCoreAsync(
            Guid serviceCaseId,
            Guid lockedAssetId,
            RequireExternalServiceRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateVendorId(request.VendorId);

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

        var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken)
            ?? throw new VendorNotFoundException(request.VendorId);

        ValidateConsistentState(serviceCase, asset);

        var previousStatus = serviceCase.Status;
        var decidedAtUtc = DateTime.UtcNow;

        serviceCase.RequireExternalService(vendor.VendorId);

        var serviceCaseEvent = CreateServiceCaseEvent(
                serviceCase,
                previousStatus,
                request.Comment,
                currentUser,
                decidedAtUtc);

        await _serviceCaseRepository.AddEventAsync(serviceCaseEvent, cancellationToken);
        await _serviceCaseRepository.SaveChangesAsync(cancellationToken);

        return MapResponse(
            serviceCase,
            asset,
            vendor,
            currentUser,
            decidedAtUtc);
    }

    private static ServiceCaseEvent CreateServiceCaseEvent(
        ServiceCase serviceCase,
        ServiceCaseStatus previousStatus,
        string? comment,
        ApplicationUser currentUser,
        DateTime occurredAtUtc)
    {
        return ServiceCaseEvent.Create(
            serviceCaseId: serviceCase.Id,
            type: ServiceCaseEventType.ExternalServiceRequired,
            previousStatus: previousStatus,
            newStatus: serviceCase.Status,
            comment: comment,
            performedByUserId: currentUser.Id,
            performedByUserName: currentUser.DisplayName,
            occurredAtUtc: occurredAtUtc);
    }

    private static RequireExternalServiceResponse MapResponse(
        ServiceCase serviceCase,
        Asset asset,
        Vendor vendor,
        ApplicationUser currentUser,
        DateTime decidedAtUtc)
    {
        if (serviceCase.VendorId != vendor.VendorId)
        {
            throw new InvalidOperationException("The selected vendor was not assigned to the service case.");
        }

        return new RequireExternalServiceResponse(
            ServiceCaseId: serviceCase.Id,
            CaseNumber: serviceCase.CaseNumber,
            AssetId: asset.Id,
            ServiceCaseStatus: serviceCase.Status,
            AssetStatus: asset.AssetStatus,
            VendorId: vendor.VendorId,
            DecidedAtUtc: decidedAtUtc,
            DecidedByUserId: currentUser.Id,
            DecidedByUserName: currentUser.DisplayName);
    }

    private static void ValidateServiceCaseId(Guid serviceCaseId)
    {
        if (serviceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
        }
    }

    private static void ValidateVendorId(Guid vendorId)
    {
        if (vendorId == Guid.Empty)
        {
            throw new ArgumentException("The vendor identifier cannot be empty.", nameof(vendorId));
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

        if (serviceCase.Status != ServiceCaseStatus.UnderCentralInspection)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "External service can only be requested while the service case is under central inspection.");
        }

        if (asset.AssetStatus != AssetStatus.In_HQ)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "External service can only be requested for an asset located at headquarters.");
        }

        if (serviceCase.VendorId.HasValue)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id, "A vendor has already been assigned to the service case.");
        }
    }
}