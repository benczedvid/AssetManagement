using System.Globalization;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.AssetHistories;
using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Stores;
using AssetManagement.Application.Users;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.AssetHistories;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Assets.UpdateAsset
{
    public sealed class UpdateAssetHandler
    {
        private const string UpdateAction = "Updated";

        private readonly IAssetRepository _assetRepository;
        private readonly IAssetHistoryRepository _assetHistoryRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IUserAccessScopeResolver _userAccessScopeResolver;
        private readonly ICurrentApplicationUserResolver  _currentApplicationUserResolver;

        public UpdateAssetHandler(
            IAssetRepository assetRepository,
            IAssetHistoryRepository assetHistoryRepository,
            IStoreRepository storeRepository,
            IApplicationUserRepository userRepository,
            IUserAccessScopeResolver userAccessScopeResolver,
            ICurrentApplicationUserResolver currentApplicationUserResolver)
        {
            ArgumentNullException.ThrowIfNull(assetRepository);
            ArgumentNullException.ThrowIfNull(assetHistoryRepository);
            ArgumentNullException.ThrowIfNull(storeRepository);
            ArgumentNullException.ThrowIfNull(userRepository);
            ArgumentNullException.ThrowIfNull(userAccessScopeResolver);
            ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);

            _assetRepository = assetRepository;
            _assetHistoryRepository = assetHistoryRepository;
            _storeRepository = storeRepository;
            _userRepository = userRepository;
            _userAccessScopeResolver = userAccessScopeResolver;
            _currentApplicationUserResolver = currentApplicationUserResolver;
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
            var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);
            var existingAsset = await GetAccessibleAssetAsync(id, accessScope, cancellationToken);
            EnsureTargetStoreIsAccessible(request.AssignedStoreId, accessScope);
            await EnsureStoreExistsAsync(request.AssignedStoreId,  cancellationToken);

            if (request.AssignedUserId.HasValue)
            {
                await EnsureUserExistsAsync(request.AssignedUserId.Value, cancellationToken);
            }

            var originalState = AssetAuditState.From(existingAsset);

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
                operatingSystemVersion: request.OperatingSystemVersion,
                warrantyEndDate: request.WarrantyEndDate);

            UpdateVendorAssignment(existingAsset, request.AssignedVendorId);

            var updatedState = AssetAuditState.From(existingAsset);

            var histories = CreateHistories(
                assetId: existingAsset.Id,
                oldState: originalState,
                newState: updatedState,
                changedBy: currentUser,
                changedAtUtc: DateTime.UtcNow);

            if (histories.Count > 0)
            {
                await _assetHistoryRepository.AddRangeAsync(histories, cancellationToken);
            }

            /*
             * Mindkét repository ugyanazt a scoped AppDbContext típust
             * használja. Ez az egyetlen SaveChangesAsync az Asset és az
             * AssetHistory módosításokat együtt menti.
             */
            await _assetRepository.SaveChangesAsync(cancellationToken);

            return Map(existingAsset);
        }

        private static IReadOnlyList<AssetHistory> CreateHistories(
            Guid assetId,
            AssetAuditState oldState,
            AssetAuditState newState,
            ApplicationUser changedBy,
            DateTime changedAtUtc)
        {
            var histories = new List<AssetHistory>();

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.AssetName),
                oldState.AssetName,
                newState.AssetName,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.AssetType),
                oldState.AssetType,
                newState.AssetType,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.AssetStatus),
                oldState.AssetStatus,
                newState.AssetStatus,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.Manufacturer),
                oldState.Manufacturer,
                newState.Manufacturer,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.Model),
                oldState.Model,
                newState.Model,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.RfidTagId),
                oldState.RfidTagId,
                newState.RfidTagId,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.MacAddress),
                oldState.MacAddress,
                newState.MacAddress,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.WiFiMacAddress),
                oldState.WiFiMacAddress,
                newState.WiFiMacAddress,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.Imei),
                oldState.Imei,
                newState.Imei,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.OperatingSystem),
                oldState.OperatingSystem,
                newState.OperatingSystem,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.OperatingSystemVersion),
                oldState.OperatingSystemVersion,
                newState.OperatingSystemVersion,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.AssignedStoreId),
                oldState.AssignedStoreId,
                newState.AssignedStoreId,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.AssignedUserId),
                oldState.AssignedUserId,
                newState.AssignedUserId,
                changedBy,
                changedAtUtc);

            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.AssignedVendorId),
                oldState.AssignedVendorId,
                newState.AssignedVendorId,
                changedBy,
                changedAtUtc);
            AddIfChanged(
                histories,
                assetId,
                nameof(Asset.WarrantyEndDate),
                oldState.WarrantyEndDate,
                newState.WarrantyEndDate,
                changedBy,
                changedAtUtc);

            return histories;
        }

        private static void AddIfChanged<T>(
            ICollection<AssetHistory> histories,
            Guid assetId,
            string propertyName,
            T oldValue,
            T newValue,
            ApplicationUser changedBy,
            DateTime changedAtUtc)
        {
            var oldText = ConvertToAuditValue(oldValue);
            var newText = ConvertToAuditValue(newValue);

            if (string.Equals(
                    oldText,
                    newText,
                    StringComparison.Ordinal))
            {
                return;
            }

            histories.Add(
                new AssetHistory(
                    assetId: assetId,
                    action: UpdateAction,
                    propertyName: propertyName,
                    oldValue: oldText,
                    newValue: newText,
                    changedByUserId: changedBy.Id,
                    changedByUserName: changedBy.DisplayName,
                    changedAtUtc: changedAtUtc));
        }

        private static string? ConvertToAuditValue<T>(T value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is Enum)
            {
                return value.ToString();
            }

            if (value is DateTime dateTime)
            {
                return dateTime
                    .ToUniversalTime()
                    .ToString("O", CultureInfo.InvariantCulture);
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset
                    .ToUniversalTime()
                    .ToString("O", CultureInfo.InvariantCulture);
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(
                    null,
                    CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        private static void EnsureTargetStoreIsAccessible(
            Guid targetStoreId,
            UserAccessScope accessScope)
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
            _ = await _storeRepository.GetByIdAsync(storeId, cancellationToken)
                ?? throw new StoreNotFoundException(storeId);
        }

        private async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
        {
            _ = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new ApplicationUserNotFoundException(userId);
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

        private async Task<Asset> GetAccessibleAssetAsync(Guid assetId, UserAccessScope accessScope, CancellationToken cancellationToken)
        {
            if (accessScope.CanAccessAllStores)
            {
                return await _assetRepository.GetForUpdateByIdAsync(assetId, cancellationToken)
                       ?? throw new AssetNotFoundException(assetId);
            }

            if (!accessScope.StoreId.HasValue)
            {
                throw new InvalidOperationException("The current user does not have a valid store assignment.");
            }

            return await _assetRepository.GetByIdAndStoreIdAsync(
                       assetId,
                       accessScope.StoreId.Value,
                       cancellationToken)
                   ?? throw new AssetNotFoundException(assetId);
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
                RfidTagId: asset.RfidTagId,
                WarrantyEndDate: asset.WarrantyEndDate);
        }

        private sealed record AssetAuditState(
            string AssetName,
            AssetType AssetType,
            AssetStatus AssetStatus,
            string Manufacturer,
            string Model,
            string? RfidTagId,
            string? MacAddress,
            string? WiFiMacAddress,
            string? Imei,
            string? OperatingSystem,
            string? OperatingSystemVersion,
            Guid AssignedStoreId,
            Guid? AssignedUserId,
            Guid? AssignedVendorId,
            DateOnly? WarrantyEndDate)
        {
            public static AssetAuditState From(Asset asset)
            {
                ArgumentNullException.ThrowIfNull(asset);

                return new AssetAuditState(
                    AssetName: asset.AssetName,
                    AssetType: asset.AssetType,
                    AssetStatus: asset.AssetStatus,
                    Manufacturer: asset.Manufacturer,
                    Model: asset.Model,
                    RfidTagId: asset.RfidTagId,
                    MacAddress: asset.MacAddress,
                    WiFiMacAddress: asset.WiFiMacAddress,
                    Imei: asset.Imei,
                    OperatingSystem: asset.OperatingSystem,
                    OperatingSystemVersion: asset.OperatingSystemVersion,
                    AssignedStoreId: asset.AssignedStoreId,
                    AssignedUserId: asset.AssignedUserId,
                    AssignedVendorId: asset.AssignedVendorId,
                    WarrantyEndDate: asset.WarrantyEndDate);
            }
        }
    }
}