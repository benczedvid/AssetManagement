using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.CaseNumbers;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.OpenServiceCase;

public sealed class OpenServiceCaseHandler
{
    private const string ServiceCaseNumberPrefix = "SC";

    private readonly IAssetRepository _assetRepository;
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IUserAccessScopeResolver _userAccessScopeResolver;
    private readonly ICaseNumberGenerator _caseNumberGenerator;
    private readonly IPackageRepository _packageRepository;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    public OpenServiceCaseHandler(
        IAssetRepository assetRepository,
        IServiceCaseRepository serviceCaseRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IUserAccessScopeResolver userAccessScopeResolver,
        ICaseNumberGenerator caseNumberGenerator,
        IPackageRepository packageRepository,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(userAccessScopeResolver);
        ArgumentNullException.ThrowIfNull(caseNumberGenerator);
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _assetRepository = assetRepository;
        _serviceCaseRepository = serviceCaseRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _userAccessScopeResolver = userAccessScopeResolver;
        _caseNumberGenerator = caseNumberGenerator;
        _packageRepository = packageRepository;
        _assetOperationExecutor = assetOperationExecutor;
    }
    /// <summary>
    /// Opens a service case within a transaction protected
    /// by an exclusive lock for the requested asset.
    /// </summary>
    /// <remarks>
    /// The asset and related business state are loaded and validated
    /// only after the asset lock has been acquired.
    ///
    /// The core operation saves its changes, and the executor commits
    /// the transaction before returning the response.
    ///
    /// Package creation and disposal workflows must use the same
    /// asset locking mechanism to prevent conflicting operations.
    /// </remarks>
    /// <param name="request">
    /// The service case creation request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lock acquisition and execution.
    /// </param>
    /// <returns>
    /// The created service case information after the transaction
    /// has been committed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the asset identifier is empty.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the asset lock cannot be acquired within
    /// the configured timeout.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the execution context does not support
    /// the protected operation.
    /// </exception>
    public Task<OpenServiceCaseResponse> HandleAsync(OpenServiceCaseRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateAssetId(request.AssetId);

        return _assetOperationExecutor.ExecuteAsync<OpenServiceCaseResponse>(
            assetIds: new[] { request.AssetId },
            operation: token => HandleCoreAsync(request, token),
            cancellationToken: cancellationToken);
    }

    public async Task<OpenServiceCaseResponse> HandleCoreAsync(OpenServiceCaseRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateAssetId(request.AssetId);

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);
        var accessScope = await _userAccessScopeResolver.ResolveAsync(cancellationToken);

        var asset = await _assetRepository.GetForUpdateByIdAsync(request.AssetId, cancellationToken)
            ?? throw new AssetNotFoundException($"The asset with identifier '{request.AssetId}' could not be found.");

        ServiceCaseAuthorization.EnsureCanManage(currentUser, asset.AssignedStoreId);

        ValidateAssetAccess(asset, accessScope);

        await EnsureAssetIsNotInActivePackageAsync(asset.Id, cancellationToken);

        ValidateAssetForServiceCase(asset);

        var initialServiceCaseStatus = ResolveInitialServiceCaseStatus(asset.AssetStatus);
        var originStoreId = ResolveOriginStoreId(asset, initialServiceCaseStatus);

        ServiceCaseAuthorization.EnsureCanManage(currentUser, originStoreId);

        var hasActiveServiceCase = await _serviceCaseRepository.HasActiveServiceCaseForAssetAsync(asset.Id, cancellationToken);

        if (hasActiveServiceCase)
        {
            throw new ServiceCaseAlreadyExistsException(asset.Id);
        }

        var nowUtc = DateTime.UtcNow;

        var caseNumber = await _caseNumberGenerator.GenerateAsync(
            prefix: ServiceCaseNumberPrefix,
            occurredAtUtc: nowUtc,
            cancellationToken: cancellationToken);

        var serviceCase = ServiceCase.Create(
            caseNumber: caseNumber,
            assetId: asset.Id,
            initialStatus: initialServiceCaseStatus,
            issueType: request.IssueType,
            otherIssueDescription: request.OtherIssueDescription,
            description: request.Description,
            originStoreId: originStoreId,
            createdByUserId: currentUser.Id,
            createdByUserName: currentUser.DisplayName,
            createdAtUtc: nowUtc);

        var serviceCaseCreatedEvent = ServiceCaseEvent.Create(
            serviceCaseId: serviceCase.Id,
            type: ServiceCaseEventType.Created,
            previousStatus: null,
            newStatus: serviceCase.Status,
            comment: null,
            performedByUserId: currentUser.Id,
            performedByUserName: currentUser.DisplayName,
            occurredAtUtc: nowUtc);

        await _serviceCaseRepository.AddAsync(serviceCase, cancellationToken);
        await _serviceCaseRepository.AddEventAsync(serviceCaseCreatedEvent, cancellationToken);

        await _serviceCaseRepository.SaveChangesAsync(cancellationToken);

        return new OpenServiceCaseResponse(
            Id: serviceCase.Id,
            CaseNumber: serviceCase.CaseNumber,
            AssetId: serviceCase.AssetId,
            Status: serviceCase.Status,
            IssueType: serviceCase.IssueType,
            OtherIssueDescription: serviceCase.OtherIssueDescription,
            Description: serviceCase.Description,
            OriginStoreId: serviceCase.OriginStoreId,
            VendorId: serviceCase.VendorId,
            AssetStatus: asset.AssetStatus,
            CreatedByUserId: serviceCase.CreatedByUserId,
            CreatedByUserName: serviceCase.CreatedByUserName,
            CreatedAtUtc: serviceCase.CreatedAtUtc);
    }

    private static void ValidateAssetId(Guid assetId)
    {
        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("The asset identifier cannot be empty.", nameof(assetId));
        }
    }

    private static void ValidateAssetAccess(Asset asset, UserAccessScope accessScope)
    {
        if (accessScope.CanAccessAllStores)
        {
            return;
        }

        if (!accessScope.StoreId.HasValue)
        {
            throw new ServiceCaseAccessDeniedException("The current user does not have a valid store access scope.");
        }

        if (asset.AssignedStoreId != accessScope.StoreId.Value)
        {
            throw new ServiceCaseAccessDeniedException("The current user cannot open a service case for an asset assigned to another store.");
        }
    }

    private static void ValidateAssetForServiceCase(Asset asset)
    {
        if (asset.AssetStatus == AssetStatus.Used_In_Store || !string.IsNullOrWhiteSpace(asset.AssignedEmployeeNumber))
        {
            throw new ServiceCaseStateConflictException("The asset must be returned by the store employee before a service case can be opened.");
        }

        if (asset.AssetStatus is not AssetStatus.In_Store and not AssetStatus.In_HQ)
        {
            throw new ServiceCaseStateConflictException("A service case can only be opened for an asset that is in a store or at headquarters.");
        }

    }

    private static ServiceCaseStatus ResolveInitialServiceCaseStatus(AssetStatus assetStatus)
    {
        return assetStatus switch
        {
            AssetStatus.In_Store => ServiceCaseStatus.AwaitingCentralReceipt,
            AssetStatus.In_HQ => ServiceCaseStatus.UnderCentralInspection,

            _ => throw new InvalidOperationException("A service case can only be opened for an asset that is in a store or at headquarters.")
        };
    }

    private static Guid? ResolveOriginStoreId(Asset asset, ServiceCaseStatus initialStatus)
    {
        return initialStatus switch
        {
            ServiceCaseStatus.AwaitingCentralReceipt => asset.AssignedStoreId,
            ServiceCaseStatus.UnderCentralInspection => null,

            _ => throw new InvalidOperationException("The initial service case status is not supported.")
        };
    }
    private async Task EnsureAssetIsNotInActivePackageAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var hasActivePackage = await _packageRepository.HasActivePackageForAssetAsync(assetId, cancellationToken);

        if (hasActivePackage)
        {
            throw new ServiceCaseStateConflictException(
                "A service case cannot be opened for an asset " +
                "included in an active package. " +
                "If a service case is required, open it before " +
                "adding the asset to a package.");
        }
    }
}