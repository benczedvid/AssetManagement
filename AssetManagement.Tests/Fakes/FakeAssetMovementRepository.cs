using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Domain.Entities.AssetMovements;

namespace AssetManagement.Tests.Fakes
{
    public sealed class FakeAssetMovementRepository
        : IAssetMovementRepository
    {
        private readonly List<AssetMovement> _assetMovements = [];
        public IReadOnlyList<AssetMovement> AssetMovements => _assetMovements.AsReadOnly();
        public int GetByIdCallCount { get; private set; }
        public int GetByAssetIdCallCount { get; private set; }
        public int GetRecentByAssetIdCallCount { get; private set; }
        public int AddCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public Guid? LastRequestedMovementId { get; private set; }
        public Guid? LastRequestedAssetId { get; private set; }
        public DateTime? LastRequestedFromUtc { get; private set; }
        public CancellationToken LastGetByIdCancellationToken { get; private set; }
        public CancellationToken LastGetByAssetIdCancellationToken { get; private set; }
        public CancellationToken LastGetRecentByAssetIdCancellationToken { get; private set; }
        public CancellationToken LastAddCancellationToken { get; private set; }
        public CancellationToken LastSaveChangesCancellationToken { get; private set; }

        public Task<AssetMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            LastRequestedMovementId = id;
            LastGetByIdCancellationToken = cancellationToken;

            var movement = _assetMovements.SingleOrDefault(currentMovement => currentMovement.Id == id);

            return Task.FromResult(movement);
        }

        public Task<IReadOnlyCollection<AssetMovement>>GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
        {
            GetByAssetIdCallCount++;
            LastRequestedAssetId = assetId;
            LastGetByAssetIdCancellationToken = cancellationToken;

            IReadOnlyCollection<AssetMovement> movements =
                [.. _assetMovements.Where(movement => movement.AssetId == assetId)];

            return Task.FromResult(movements);
        }

        public Task<IReadOnlyList<AssetMovement>>GetRecentByAssetIdAsync(Guid assetId, DateTime fromUtc, CancellationToken cancellationToken = default)
        {
            GetRecentByAssetIdCallCount++;
            LastRequestedAssetId = assetId;
            LastRequestedFromUtc = fromUtc;

            LastGetRecentByAssetIdCancellationToken = cancellationToken;

            IReadOnlyList<AssetMovement> movements = [.. _assetMovements
                    .Where(movement =>
                        movement.AssetId == assetId &&
                        movement.CreatedAtUtc >= fromUtc)
                    .OrderByDescending(
                        movement => movement.CreatedAtUtc)];

            return Task.FromResult(movements);
        }

        public Task AddAsync(AssetMovement assetMovement, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(assetMovement);

            AddCallCount++;
            LastAddCancellationToken = cancellationToken;

            _assetMovements.Add(assetMovement);

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            LastSaveChangesCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }

        public void Seed(AssetMovement assetMovement)
        {
            ArgumentNullException.ThrowIfNull(assetMovement);

            _assetMovements.Add(assetMovement);
        }

        public void Seed(
            IEnumerable<AssetMovement> assetMovements)
        {
            ArgumentNullException.ThrowIfNull(assetMovements);

            foreach (var assetMovement in assetMovements)
            {
                Seed(assetMovement);
            }
        }
    }
}