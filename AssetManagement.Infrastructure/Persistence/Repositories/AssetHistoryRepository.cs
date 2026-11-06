using AssetManagement.Application.Common.Interfaces.AssetHistories;
using AssetManagement.Domain.Entities.AssetHistories;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories
{
    public sealed class AssetHistoryRepository : IAssetHistoryRepository
    {
        private readonly AppDbContext _appDbContext;

        public AssetHistoryRepository(AppDbContext appDbContext)
        {
            ArgumentNullException.ThrowIfNull(appDbContext);

            _appDbContext = appDbContext;
        }

        public async Task AddRangeAsync(IEnumerable<AssetHistory> assetHistories, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(assetHistories);

            await _appDbContext.AssetHistory.AddRangeAsync(assetHistories, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _appDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AssetHistory>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
        {
            if (assetId == Guid.Empty)
            {
                throw new ArgumentException("The asset identifier cannot be empty.", nameof(assetId));
            }

            return await _appDbContext.AssetHistory
                .AsNoTracking()
                .Where(history => history.AssetId == assetId)
                .OrderByDescending(history => history.ChangedAtUtc)
                .ThenByDescending(history => history.Id)
                .ToListAsync(cancellationToken);
        }
    }
}