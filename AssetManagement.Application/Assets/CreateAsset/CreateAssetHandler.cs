using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Stores;
using AssetManagement.Application.Users;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Assets.CreateAsset
{
    public sealed class CreateAssetHandler
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IApplicationUserRepository _userRepository;

        public CreateAssetHandler(
            IAssetRepository assetRepository,
            IStoreRepository storeRepository,
            IApplicationUserRepository userRepository)
        {
            ArgumentNullException.ThrowIfNull(assetRepository);
            ArgumentNullException.ThrowIfNull(storeRepository);
            ArgumentNullException.ThrowIfNull(userRepository);

            _assetRepository = assetRepository;
            _storeRepository = storeRepository;
            _userRepository = userRepository;
        }

        public async Task<CreateAssetResponse> HandleAsync(CreateAssetRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await EnsureStoreExistsAsync( storeId: request.AssignedStoreId, cancellationToken: cancellationToken);

            if (request.AssignedUserId.HasValue)
            {
                await EnsureUserExistsAsync(userId: request.AssignedUserId.Value, cancellationToken: cancellationToken);
            }

            await EnsureSerialNumberIsUniqueAsync(serialNumber: request.SerialNumber, cancellationToken: cancellationToken);

            var newAsset = Asset.Create(
                assetName: request.AssetName,
                assetType: request.AssetType,
                assetStatus: request.AssetStatus,
                manufacturer: request.Manufacturer,
                model: request.Model,
                serialNumber: request.SerialNumber,
                assignedStoreId: request.AssignedStoreId,
                rfidTagId: request.RfidTagId,
                assignedVendorId: request.AssignedVendorId,
                assignedEmployeeNumber: request.AssignedEmployeeNumber,
                assignedUserId: request.AssignedUserId,
                macAddress: request.MacAddress,
                wifiMacAddress: request.WifimacAddress,
                imei: request.Imei,
                operatingSystem: request.OperatingSystem,
                operatingSystemVersion:
                    request.OperatingSystemVersion);

            await _assetRepository.AddAsync(newAsset, cancellationToken);
            await _assetRepository.SaveChangesAsync(cancellationToken);

            return MapToResponse(newAsset);
        }

        private async Task EnsureStoreExistsAsync(Guid storeId, CancellationToken cancellationToken)
        {
            _ = await _storeRepository.GetByIdAsync(storeId, cancellationToken) ?? throw new StoreNotFoundException(storeId);
        }

        private async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
        {
            _ = await _userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new ApplicationUserNotFoundException(userId);
        }

        private async Task EnsureSerialNumberIsUniqueAsync(string serialNumber, CancellationToken cancellationToken)
        {
            var existingAsset = await _assetRepository.GetBySerialNumberAsync(serialNumber, cancellationToken);

            if (existingAsset is not null)
            {
                throw new SerialNumberAlreadyExistsException(existingAsset.SerialNumber);
            }
        }

        private static CreateAssetResponse MapToResponse(Asset asset)
        {
            return new CreateAssetResponse(
                AssetId: asset.Id,
                AssetName: asset.AssetName,
                AssetType: asset.AssetType,
                AssetStatus: asset.AssetStatus,
                Manufacturer: asset.Manufacturer,
                Model: asset.Model,
                SerialNumber: asset.SerialNumber,
                MacAddress: asset.MacAddress,
                WifimacAddress: asset.WiFiMacAddress,
                Imei: asset.Imei,
                OperatingSystem: asset.OperatingSystem,
                OperatingSystemVersion: asset.OperatingSystemVersion,
                AssignedEmployeeNumber: asset.AssignedEmployeeNumber,
                AssignedStoreId: asset.AssignedStoreId,
                AssignedUserId: asset.AssignedUserId,
                CreatedAtUtc: asset.CreatedAtUtc,
                AssignedVendorId: asset.AssignedVendorId,
                RfidTagId: asset.RfidTagId);
        }
    }
}