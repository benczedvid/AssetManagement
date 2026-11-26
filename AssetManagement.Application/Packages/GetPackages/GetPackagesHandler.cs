using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Packages.Exceptions;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.GetPackages;

/// <summary>
/// Handles requests for retrieving packages visible to the current user.
/// </summary>
public sealed class GetPackagesHandler
{
    private readonly IPackageRepository _packageRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IUserAccessScopeResolver _userAccessScopeResolver;

    /// <summary>
    /// Initializes a new instance of the GetPackagesHandler class.
    /// </summary>
    public GetPackagesHandler(
        IPackageRepository packageRepository,
        IAssetRepository assetRepository,
        IUserAccessScopeResolver userAccessScopeResolver)
    {
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(userAccessScopeResolver);

        _packageRepository = packageRepository;
        _assetRepository = assetRepository;
        _userAccessScopeResolver = userAccessScopeResolver;
    }

    /// <summary>
    /// Retrieves packages visible within the current user's store scope,
    /// including asset names and serial numbers.
    /// </summary>
    public async Task<GetPackagesResponse> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var accessScope = await _userAccessScopeResolver
            .ResolveAsync(cancellationToken);

        var storeId = ResolveStoreFilter(accessScope);

        var packages = await _packageRepository.GetListAsync(
            storeId,
            cancellationToken);

        var orderedPackages = packages
            .OrderByDescending(package => package.CreatedAtUtc)
            .ThenBy(package => package.PackageNumber)
            .ToArray();

        /*
         * Collect unique asset identifiers from packages
         * visible to the current user.
         */
        var assetIds = orderedPackages
            .SelectMany(package => package.Items)
            .Select(item => item.AssetId)
            .Distinct()
            .ToArray();

        /*
         * Load all required assets in a single query.
         */
        var assets = assetIds.Length == 0
            ? Array.Empty<Asset>()
            : await _assetRepository.GetByIdsAsync(
                assetIds,
                cancellationToken);

        /*
         * Build a lookup for efficient asset mapping.
         */
        var assetsById = assets.ToDictionary(
            asset => asset.Id);

        /*
         * Map packages and their assets to response models.
         */
        var items = orderedPackages
            .Select(package => MapListItem(
                package,
                assetsById))
            .ToArray();

        return new GetPackagesResponse(
            Items: items);
    }

    /// <summary>
    /// Maps a package to a list response item.
    /// </summary>
    private static GetPackageListItemResponse MapListItem(
        Package package,
        IReadOnlyDictionary<Guid, Asset> assetsById)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(assetsById);

        var packageAssets = package.Items
            .OrderBy(item => item.AddedAtUtc)
            .ThenBy(item => item.Id)
            .Select(item =>
            {
                if (!assetsById.TryGetValue(
                    item.AssetId,
                    out var asset))
                {
                    throw new InvalidOperationException(
                        $"Asset '{item.AssetId}' referenced by package " +
                        $"'{package.Id}' was not found.");
                }

                return new GetPackageListAssetResponse(
                    AssetId: asset.Id,
                    AssetName: asset.AssetName,
                    SerialNumber: asset.SerialNumber);
            })
            .ToArray();

        return new GetPackageListItemResponse(
            Id: package.Id,
            PackageNumber: package.PackageNumber,
            Status: package.Status,

            SourceStoreId: package.SourceStoreId,
            SourceStoreNumber: package.SourceStore.StoreNumber,
            SourceStoreName: package.SourceStore.Name,

            DestinationStoreId: package.DestinationStoreId,
            DestinationStoreNumber: package.DestinationStore.StoreNumber,
            DestinationStoreName: package.DestinationStore.Name,

            CourierUserId: package.CourierUserId,
            CourierUserName: package.CourierUser?.DisplayName,

            AssetCount: package.Items.Count,

            CreatedAtUtc: package.CreatedAtUtc,
            CreatedByUserId: package.CreatedByUserId,

            DispatchedAtUtc: package.DispatchedAtUtc,
            ReceivedAtUtc: package.ReceivedAtUtc,

            Assets: packageAssets);
    }

    /// <summary>
    /// Resolves the store filter from the current user's access scope.
    /// </summary>
    private static Guid? ResolveStoreFilter(
        UserAccessScope accessScope)
    {
        ArgumentNullException.ThrowIfNull(accessScope);

        if (accessScope.CanAccessAllStores)
        {
            return null;
        }

        if (!accessScope.StoreId.HasValue)
        {
            throw new PackageAccessDeniedException(
                "The current user does not have a valid store access scope.");
        }

        if (accessScope.StoreId.Value == Guid.Empty)
        {
            throw new PackageAccessDeniedException(
                "The current user has an invalid store access scope.");
        }

        return accessScope.StoreId.Value;
    }
}