using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.GetAsset
{
    public sealed class GetAssetByIdHandler
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IUserAccessScopeResolver _userAccessScopeResolver;

        public GetAssetByIdHandler(
            IAssetRepository assetRepository,
            IUserAccessScopeResolver userAccessScopeResolver)
        {
            ArgumentNullException.ThrowIfNull(assetRepository);
            ArgumentNullException.ThrowIfNull(userAccessScopeResolver);

            _assetRepository = assetRepository;
            _userAccessScopeResolver = userAccessScopeResolver;
        }

        public async Task<GetAssetResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("The asset identifier cannot be empty.", nameof(id));
            }

            var accessScope = await _userAccessScopeResolver.ResolveAsync(cancellationToken);

            Asset? asset;

            if (accessScope.CanAccessAllStores)
            {
                asset = await _assetRepository.GetByIdAsync(id, cancellationToken);
            }
            else
            {
                if (!accessScope.StoreId.HasValue)
                {
                    throw new InvalidOperationException("The current user does not have a valid store assignment.");
                }

                asset = await _assetRepository.GetByIdAndStoreIdAsync(id, accessScope.StoreId.Value, cancellationToken);
            }

            if (asset is null)
            {
                throw new AssetNotFoundException(id);
            }

            return Map(asset);
        }

        private static GetAssetResponse Map(Asset asset)
        {
            return new GetAssetResponse(
                AssetId: asset.Id,
                AssetName: asset.AssetName,
                AssetType: asset.AssetType,
                AssetStatus: asset.AssetStatus,
                Manufacturer: asset.Manufacturer,
                Model: asset.Model,
                SerialNumber: asset.SerialNumber,
                MacAddress: asset.MacAddress,
                WifiMacAddress: asset.WiFiMacAddress,
                Imei: asset.Imei,
                OperatingSystem: asset.OperatingSystem,
                OperatingSystemVersion: asset.OperatingSystemVersion,
                AssignedStoreId: asset.AssignedStoreId,
                AssignedUserId: asset.AssignedUserId,
                CreatedAtUtc: asset.CreatedAtUtc,
                AssignedVendorId: asset.AssignedVendorId,
                RfidTagId: asset.RfidTagId);
        }
    }
}