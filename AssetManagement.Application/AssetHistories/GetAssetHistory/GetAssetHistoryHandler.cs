using AssetManagement.Application.Assets;
using AssetManagement.Application.Assets.UpdateAsset;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.AssetHistories;
using AssetManagement.Application.Common.Interfaces.Assets;

namespace AssetManagement.Application.AssetHistories.GetAssetHistory
{
    public sealed class GetAssetHistoryHandler
    {
        private readonly IAssetHistoryRepository
            _assetHistoryRepository;

        private readonly IAssetRepository _assetRepository;

        private readonly IUserAccessScopeResolver
            _userAccessScopeResolver;

        public GetAssetHistoryHandler(
            IAssetHistoryRepository assetHistoryRepository,
            IAssetRepository assetRepository,
            IUserAccessScopeResolver userAccessScopeResolver)
        {
            ArgumentNullException.ThrowIfNull(
                assetHistoryRepository);

            ArgumentNullException.ThrowIfNull(assetRepository);

            ArgumentNullException.ThrowIfNull(
                userAccessScopeResolver);

            _assetHistoryRepository = assetHistoryRepository;
            _assetRepository = assetRepository;
            _userAccessScopeResolver = userAccessScopeResolver;
        }

        public async Task<IReadOnlyList<AssetHistoryResponse>>
            HandleAsync(
                Guid assetId,
                CancellationToken cancellationToken = default)
        {
            if (assetId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The asset identifier cannot be empty.",
                    nameof(assetId));
            }

            var accessScope =
                await _userAccessScopeResolver.ResolveAsync(
                    cancellationToken);

            await EnsureAssetIsAccessibleAsync(
                assetId,
                accessScope,
                cancellationToken);

            var histories =
                await _assetHistoryRepository.GetByAssetIdAsync(
                    assetId,
                    cancellationToken);

            return histories
                .Select(history => new AssetHistoryResponse(
                    Id: history.Id,
                    AssetId: history.AssetId,
                    Action: history.Action,
                    PropertyName: history.PropertyName,
                    OldValue: history.OldValue,
                    NewValue: history.NewValue,
                    ChangedByUserId:
                        history.ChangedByUserId,
                    ChangedByUserName:
                        history.ChangedByUserName,
                    ChangedAtUtc: history.ChangedAtUtc))
                .ToList();
        }

        private async Task EnsureAssetIsAccessibleAsync(
            Guid assetId,
            UserAccessScope accessScope,
            CancellationToken cancellationToken)
        {
            if (accessScope.CanAccessAllStores)
            {
                var asset = await _assetRepository.GetByIdAsync(
                    assetId,
                    cancellationToken);

                if (asset is null)
                {
                    throw new AssetNotFoundException(assetId);
                }

                return;
            }

            if (!accessScope.StoreId.HasValue)
            {
                throw new InvalidOperationException(
                    "The current user does not have a valid store assignment.");
            }

            var storeAsset =
                await _assetRepository.GetByIdAndStoreIdAsync(
                    assetId,
                    accessScope.StoreId.Value,
                    cancellationToken);

            if (storeAsset is null)
            {
                throw new AssetNotFoundException(assetId);
            }
        }
    }
}