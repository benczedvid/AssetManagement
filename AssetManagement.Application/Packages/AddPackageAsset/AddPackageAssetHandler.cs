using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Packages.Exceptions;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Packages;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.Stores;

namespace AssetManagement.Application.Packages.AddPackageAsset;

/// <summary>
/// Handles adding an asset to an existing package.
/// </summary>
/// <remarks>
/// Assets can only be added while the package is in Created status.
///
/// The operation uses package-first locking followed by exclusive
/// asset-level locks.
///
/// Asset and service case states are validated but not modified.
/// </remarks>
public sealed class AddPackageAssetHandler
{
    private const string HeadquartersStoreNumber = "999";

    private readonly IPackageRepository _packageRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IUserAccessScopeResolver _userAccessScopeResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public AddPackageAssetHandler(
        IPackageRepository packageRepository,
        IAssetRepository assetRepository,
        IStoreRepository storeRepository,
        IServiceCaseRepository serviceCaseRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IUserAccessScopeResolver userAccessScopeResolver,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(userAccessScopeResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _packageRepository = packageRepository;
        _assetRepository = assetRepository;
        _storeRepository = storeRepository;
        _serviceCaseRepository = serviceCaseRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _userAccessScopeResolver = userAccessScopeResolver;
        _assetOperationExecutor = assetOperationExecutor;
    }

    /// <summary>
    /// Adds an asset to an existing package.
    /// </summary>
    /// <param name="packageId">
    /// The identifier of the package.
    /// </param>
    /// <param name="request">
    /// The asset addition request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The updated package information.
    /// </returns>
    public Task<AddPackageAssetResponse> HandleAsync(
        Guid packageId,
        AddPackageAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(
            packageId,
            nameof(packageId));

        ArgumentNullException.ThrowIfNull(request);

        ValidateIdentifier(
            request.AssetId,
            nameof(request.AssetId));

        return _assetOperationExecutor
            .ExecutePackageAsync<AddPackageAssetResponse>(
                packageId: packageId,

                loadAssetIds: token =>
                    LoadAssetIdsAsync(
                        packageId,
                        request.AssetId,
                        token),

                operation: (lockedAssetIds, token) =>
                    HandleCoreAsync(
                        packageId,
                        request,
                        lockedAssetIds,
                        token),

                cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads the asset identifiers requiring exclusive locks.
    /// </summary>
    /// <remarks>
    /// This method must only perform no-tracking reads.
    /// It must not modify or save entities.
    /// </remarks>
    private async Task<IReadOnlyCollection<Guid>> LoadAssetIdsAsync(
        Guid packageId,
        Guid newAssetId,
        CancellationToken cancellationToken)
    {
        var package = await _packageRepository
            .GetByIdAsync(
                packageId,
                cancellationToken)
            ?? throw new PackageNotFoundException(packageId);

        return package.Items
            .Select(item => item.AssetId)
            .Append(newAssetId)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Executes the protected package modification.
    /// </summary>
    private async Task<AddPackageAssetResponse> HandleCoreAsync(
        Guid packageId,
        AddPackageAssetRequest request,
        IReadOnlyCollection<Guid> lockedAssetIds,
        CancellationToken cancellationToken)
    {
        var currentUser = await _currentApplicationUserResolver
            .ResolveAsync(cancellationToken);

        var accessScope = await _userAccessScopeResolver
            .ResolveAsync(cancellationToken);

        var package = await _packageRepository
            .GetForUpdateByIdAsync(
                packageId,
                cancellationToken)
            ?? throw new PackageNotFoundException(packageId);

        ValidatePackageStatus(package);

        ValidateLockedAssets(
            package,
            request.AssetId,
            lockedAssetIds);

        if (package.Items.Any(
            item => item.AssetId == request.AssetId))
        {
            throw new PackageStateConflictException(
                "The asset is already included in the package.");
        }

        var sourceStore = await _storeRepository
            .GetByIdAsync(
                package.SourceStoreId,
                cancellationToken)
            ?? throw new PackageStoreNotFoundException(
                package.SourceStoreId);

        var destinationStore = await _storeRepository
            .GetByIdAsync(
                package.DestinationStoreId,
                cancellationToken)
            ?? throw new PackageStoreNotFoundException(
                package.DestinationStoreId);

        ValidateRoute(
            sourceStore,
            destinationStore);

        ValidateSourceStoreAccess(
            sourceStore,
            accessScope);

        var asset = await _assetRepository
            .GetForUpdateByIdAsync(
                request.AssetId,
                cancellationToken)
            ?? throw new PackageAssetNotFoundException(
                request.AssetId);

        ValidateAssetForPackage(
            asset,
            sourceStore,
            destinationStore);

        var hasActivePackage = await _packageRepository
            .HasActivePackageForAssetAsync(
                asset.Id,
                cancellationToken);

        if (hasActivePackage)
        {
            throw new PackageStateConflictException(
                $"Asset '{asset.Id}' is already assigned " +
                "to an active package.");
        }

        await ValidateServiceCaseForPackageAsync(
            asset,
            sourceStore,
            destinationStore,
            cancellationToken);

        var nowUtc = DateTime.UtcNow;

        try
        {
            package.AddAsset(
                assetId: asset.Id,
                performedByUserId: currentUser.Id,
                performedByUserName: currentUser.DisplayName,
                occurredAtUtc: nowUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(
                exception.Message);
        }

        await _packageRepository.SaveChangesAsync(
            cancellationToken);

        return new AddPackageAssetResponse(
            PackageId: package.Id,
            PackageNumber: package.PackageNumber,
            Status: package.Status,
            AssetId: asset.Id,
            AssetCount: package.Items.Count,
            AddedAtUtc: nowUtc);
    }

    /// <summary>
    /// Validates that the package is still editable.
    /// </summary>
    private static void ValidatePackageStatus(
        Package package)
    {
        if (package.Status != PackageStatus.Created)
        {
            throw new PackageStateConflictException(
                "Assets can only be added to a created package.");
        }
    }

    /// <summary>
    /// Ensures that all current and requested assets
    /// are protected by the executor.
    /// </summary>
    private static void ValidateLockedAssets(
        Package package,
        Guid newAssetId,
        IReadOnlyCollection<Guid> lockedAssetIds)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(lockedAssetIds);

        var lockedAssetIdSet = lockedAssetIds.ToHashSet();

        if (!lockedAssetIdSet.Contains(newAssetId))
        {
            throw new InvalidOperationException(
                "The requested asset is not protected by an exclusive lock.");
        }

        if (package.Items.Any(
            item => !lockedAssetIdSet.Contains(item.AssetId)))
        {
            throw new PackageStateConflictException(
                "The package contents changed during lock acquisition.");
        }
    }

    /// <summary>
    /// Validates the package route.
    /// </summary>
    private static void ValidateRoute(
        Store sourceStore,
        Store destinationStore)
    {
        ArgumentNullException.ThrowIfNull(sourceStore);
        ArgumentNullException.ThrowIfNull(destinationStore);

        if (sourceStore.Id == destinationStore.Id)
        {
            throw new PackageStateConflictException(
                "Source and destination stores must be different.");
        }

        var sourceIsHeadquarters =
            IsHeadquarters(sourceStore);

        var destinationIsHeadquarters =
            IsHeadquarters(destinationStore);

        if (sourceIsHeadquarters == destinationIsHeadquarters)
        {
            throw new PackageStateConflictException(
                "A package must be transported between a store and headquarters.");
        }
    }

    /// <summary>
    /// Validates the current user's source store access.
    /// </summary>
    private static void ValidateSourceStoreAccess(
        Store sourceStore,
        UserAccessScope accessScope)
    {
        ArgumentNullException.ThrowIfNull(sourceStore);
        ArgumentNullException.ThrowIfNull(accessScope);

        if (accessScope.CanAccessAllStores)
        {
            return;
        }

        if (!accessScope.StoreId.HasValue ||
            accessScope.StoreId.Value == Guid.Empty)
        {
            throw new PackageAccessDeniedException(
                "The current user does not have a valid store access scope.");
        }

        if (accessScope.StoreId.Value != sourceStore.Id)
        {
            throw new PackageAccessDeniedException(
                "The current user cannot modify a package from another store.");
        }
    }

    /// <summary>
    /// Validates the asset against the package route.
    /// </summary>
    private static void ValidateAssetForPackage(
        Asset asset,
        Store sourceStore,
        Store destinationStore)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(sourceStore);
        ArgumentNullException.ThrowIfNull(destinationStore);

        var sourceIsHeadquarters =
            IsHeadquarters(sourceStore);

        ValidateAssetStoreAssignment(
            asset,
            sourceStore,
            destinationStore,
            sourceIsHeadquarters);

        if (asset.AssetStatus == AssetStatus.Disposed)
        {
            throw new PackageStateConflictException(
                $"Disposed asset '{asset.Id}' cannot be added to a package.");
        }

        if (!string.IsNullOrWhiteSpace(
            asset.AssignedEmployeeNumber))
        {
            throw new PackageStateConflictException(
                $"Asset '{asset.Id}' must be returned by the assigned employee " +
                "before it can be added to a package.");
        }

        if (asset.AssignedUserId.HasValue)
        {
            throw new PackageStateConflictException(
                $"Asset '{asset.Id}' must be unassigned from the application user " +
                "before it can be added to a package.");
        }

        if (sourceIsHeadquarters &&
            asset.AssetStatus != AssetStatus.In_HQ)
        {
            throw new PackageStateConflictException(
                $"Asset '{asset.Id}' must be located at headquarters.");
        }

        if (!sourceIsHeadquarters &&
            asset.AssetStatus != AssetStatus.In_Store)
        {
            throw new PackageStateConflictException(
                $"Asset '{asset.Id}' must be located at the source store.");
        }
    }

    /// <summary>
    /// Validates the asset's assigned store.
    /// </summary>
    private static void ValidateAssetStoreAssignment(
        Asset asset,
        Store sourceStore,
        Store destinationStore,
        bool sourceIsHeadquarters)
    {
        if (sourceIsHeadquarters)
        {
            if (asset.AssignedStoreId != destinationStore.Id)
            {
                throw new PackageStateConflictException(
                    $"Asset '{asset.Id}' is not assigned to the package destination store.");
            }

            return;
        }

        if (asset.AssignedStoreId != sourceStore.Id)
        {
            throw new PackageStateConflictException(
                $"Asset '{asset.Id}' is not assigned to the package source store.");
        }
    }

    /// <summary>
    /// Validates an optional active service case.
    /// </summary>
    private async Task ValidateServiceCaseForPackageAsync(
        Asset asset,
        Store sourceStore,
        Store destinationStore,
        CancellationToken cancellationToken)
    {
        var serviceCase = await _serviceCaseRepository
            .GetActiveByAssetIdAsync(
                asset.Id,
                cancellationToken);

        if (serviceCase is null)
        {
            return;
        }

        if (IsHeadquarters(destinationStore))
        {
            if (serviceCase.Status !=
                ServiceCaseStatus.AwaitingCentralReceipt)
            {
                throw new PackageStateConflictException(
                    $"The active service case for asset '{asset.Id}' " +
                    "must be awaiting central receipt before the asset " +
                    "can be added to a package destined for headquarters.");
            }

            if (serviceCase.OriginStoreId != sourceStore.Id)
            {
                throw new PackageStateConflictException(
                    $"The package source does not match the active " +
                    $"service case origin store for asset '{asset.Id}'.");
            }

            return;
        }

        if (serviceCase.Status != ServiceCaseStatus.AwaitingReturn)
        {
            throw new PackageStateConflictException(
                $"The active service case for asset '{asset.Id}' " +
                "must be awaiting return before the asset can be " +
                "added to a package destined for a store.");
        }

        if (serviceCase.OriginStoreId != destinationStore.Id)
        {
            throw new PackageStateConflictException(
                $"The package destination does not match the active " +
                $"service case origin store for asset '{asset.Id}'.");
        }
    }

    /// <summary>
    /// Determines whether a store represents headquarters.
    /// </summary>
    private static bool IsHeadquarters(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        return string.Equals(
            store.StoreNumber,
            HeadquartersStoreNumber,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates a required identifier.
    /// </summary>
    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(
                "Identifier cannot be empty.",
                parameterName);
        }
    }
}