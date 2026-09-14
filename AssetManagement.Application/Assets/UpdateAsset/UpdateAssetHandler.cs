using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Stores;
using AssetManagement.Application.Users;
using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.UpdateAsset
{
    public sealed class UpdateAssetHandler
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IUserAccessScopeResolver _userAccessScopeResolver;

        public UpdateAssetHandler(
            IAssetRepository assetRepository,
            IStoreRepository storeRepository,
            IApplicationUserRepository userRepository,
            IUserAccessScopeResolver userAccessScopeResolver)
        {
            ArgumentNullException.ThrowIfNull(assetRepository);
            ArgumentNullException.ThrowIfNull(storeRepository);
            ArgumentNullException.ThrowIfNull(userRepository);
            ArgumentNullException.ThrowIfNull(userAccessScopeResolver);

            _assetRepository = assetRepository;
            _storeRepository = storeRepository;
            _userRepository = userRepository;
            _userAccessScopeResolver = userAccessScopeResolver;
        }

        public async Task<UpdateAssetResponse> HandleAsync(
            Guid id,
            UpdateAssetRequest request,
            CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("The asset identifier cannot be empty.", nameof(id));
            }

            ArgumentNullException.ThrowIfNull(request);

            var accessScope = await _userAccessScopeResolver.ResolveAsync(cancellationToken);
            var existingAsset = await _assetRepository.GetForUpdateByIdAsync(id, cancellationToken) ?? throw new AssetNotFoundException(id);

            EnsureAssetIsAccessible(existingAsset, accessScope);
            EnsureTargetStoreIsAccessible(request.AssignedStoreId, accessScope);

            await EnsureStoreExistsAsync(request.AssignedStoreId, cancellationToken);

            if (request.AssignedUserId.HasValue)
            {
                await EnsureUserExistsAsync(request.AssignedUserId.Value, cancellationToken);
            }

            existingAsset.UpdateDetails(
                assetName: request.AssetName,
                assetType: request.AssetType,
                assetStatus: request.AssetStatus,
                manufacturer: request.Manufacturer,
                model: request.Model,
                assignedStoreId: request.AssignedStoreId,
                rfidTagId: request.RfIdTagId,
                assignedUserId: request.AssignedUserId,
                macAddress: request.MacAddress,
                wifiMacAddress: request.WifiMacAddress,
                imei: request.Imei,
                operatingSystem: request.OperatingSystem,
                operatingSystemVersion: request.OperatingSystemVersion);

            UpdateVendorAssignment(existingAsset, request.AssignedVendorId);

            await _assetRepository.SaveChangesAsync(cancellationToken);

            return Map(existingAsset);
        }

        private static void EnsureAssetIsAccessible(Asset asset, UserAccessScope accessScope)
        {
            if (accessScope.CanAccessAllStores)
            {
                return;
            }

            if (!accessScope.StoreId.HasValue)
            {
                throw new InvalidOperationException("The current user does not have a valid store assignment.");
            }

            if (asset.AssignedStoreId != accessScope.StoreId.Value)
            {
                throw new AssetNotFoundException(asset.Id);
            }
        }

        private static void EnsureTargetStoreIsAccessible(Guid targetStoreId, UserAccessScope accessScope)
        {
            if (accessScope.CanAccessAllStores)
            {
                return;
            }

            if (!accessScope.StoreId.HasValue)
            {
                throw new InvalidOperationException("The current user does not have a valid store assignment.");
            }

            if (targetStoreId != accessScope.StoreId.Value)
            {
                throw new UnauthorizedAccessException("The current user cannot move an asset to another store.");
            }
        }

        private async Task EnsureStoreExistsAsync(Guid storeId, CancellationToken cancellationToken)
        {
            _ = await _storeRepository.GetByIdAsync(storeId, cancellationToken) ?? throw new StoreNotFoundException(storeId);
        }

        private async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
        {
            _ = await _userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new ApplicationUserNotFoundException(userId);
        }

        private static void UpdateVendorAssignment(Asset asset, Guid? vendorId)
        {
            if (vendorId.HasValue)
            {
                asset.AssignVendor(vendorId.Value);
                return;
            }

            asset.RemoveVendor();
        }

        private static UpdateAssetResponse Map(Asset asset)
        {
            return new UpdateAssetResponse(
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
                AssignedVendorId: asset.AssignedVendorId,
                RfidTagId: asset.RfidTagId);
        }
    }
}