using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Stores;

namespace AssetManagement.Application.Dashboard.GetStoreDashboard;

public sealed class GetStoreDashboardHandler
{
    private readonly IStoreRepository _storeRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IUserAccessScopeResolver _userAccessScopeResolver;

    public GetStoreDashboardHandler(
        IStoreRepository storeRepository,
        IAssetRepository assetRepository,
        IUserAccessScopeResolver userAccessScopeResolver)
    {
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(userAccessScopeResolver);

        _storeRepository = storeRepository;
        _assetRepository = assetRepository;
        _userAccessScopeResolver = userAccessScopeResolver;
    }

    public async Task<IReadOnlyList<StoreDashboardResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var accessScope = await _userAccessScopeResolver.ResolveAsync(cancellationToken);

        IReadOnlyList<Store> stores;
        IReadOnlyList<Asset> assets;

        if (accessScope.CanAccessAllStores)
        {
            stores = await _storeRepository.GetAllAsync(cancellationToken);

            assets = await _assetRepository.GetAllAsync(cancellationToken);
        }
        else
        {
            if (!accessScope.StoreId.HasValue)
            {
                throw new InvalidOperationException("The current user does not have a valid store assignment.");
            }

            var storeId = accessScope.StoreId.Value;

            var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);

            if (store is null)
            {
                throw new InvalidOperationException("The store assigned to the current user was not found.");
            }

            stores = [store];

            assets = await _assetRepository.GetAllByStoreIdAsync(storeId, cancellationToken);
        }

        var activePdtsByStoreId = assets
            .Where(asset => asset.AssetType == AssetType.PDT && asset.AssetStatus != AssetStatus.Disposed)
            .GroupBy(asset => asset.AssignedStoreId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        return stores
            .OrderBy(store => store.StoreNumber)
            .Select(store =>
            {
                var storePdts =
                    activePdtsByStoreId.TryGetValue(store.Id, out var pdts) ? pdts : [];

                return new StoreDashboardResponse(
                    StoreId: store.Id,
                    StoreNumber: store.StoreNumber,
                    StoreName: store.Name,
                    TotalPDTs: storePdts.Length,
                    UsedPDTs: storePdts.Count(asset => asset.AssetStatus == AssetStatus.Used_In_Store),
                    ServicePDTs: storePdts.Count(asset => asset.AssetStatus == AssetStatus.In_Service),
                    AvailablePDTs: storePdts.Count(asset => asset.AssetStatus == AssetStatus.In_Store));
            })
            .ToArray();
    }
}