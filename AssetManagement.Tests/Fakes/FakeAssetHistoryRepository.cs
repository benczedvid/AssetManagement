using AssetManagement.Application.Common.Interfaces.AssetHistories;
using AssetManagement.Domain.Entities.AssetHistories;

namespace AssetManagement.Tests.Fakes
{
    public sealed class FakeAssetHistoryRepository
        : IAssetHistoryRepository
    {
        private readonly List<AssetHistory> _assetHistories = [];
        public IReadOnlyList<AssetHistory> AssetHistories => _assetHistories;
        public int AddRangeCallCount { get; private set; }
        public int GetByAssetIdCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public CancellationToken LastAddRangeCancellationToken { get; private set; }
        public CancellationToken LastGetByAssetIdCancellationToken { get; private set; }
        public CancellationToken LastSaveChangesCancellationToken { get; private set; }

        public Task AddRangeAsync(IEnumerable<AssetHistory> assetHistories, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(assetHistories);

            AddRangeCallCount++;
            LastAddRangeCancellationToken = cancellationToken;

            _assetHistories.AddRange(assetHistories);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AssetHistory>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
        {
            if (assetId == Guid.Empty)
            {
                throw new ArgumentException("The asset identifier cannot be empty.",nameof(assetId));
            }

            GetByAssetIdCallCount++;
            LastGetByAssetIdCancellationToken = cancellationToken;

            IReadOnlyList<AssetHistory> result = _assetHistories
                .Where(history => history.AssetId == assetId)
                .OrderByDescending(history => history.ChangedAtUtc)
                .ThenByDescending(history => history.Id)
                .ToList();

            return Task.FromResult(result);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            LastSaveChangesCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }
    }
}