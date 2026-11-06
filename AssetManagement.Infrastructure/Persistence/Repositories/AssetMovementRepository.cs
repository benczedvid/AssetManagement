using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Domain.Entities.AssetMovements;
using Microsoft.EntityFrameworkCore;


namespace AssetManagement.Infrastructure.Persistence.Repositories
{
    public sealed class AssetMovementRepository : IAssetMovementRepository
    {
        private readonly AppDbContext _appDbContext;

        public AssetMovementRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task AddAsync(AssetMovement assetMovement, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(assetMovement, nameof(assetMovement));
            await _appDbContext.AssetMovement.AddAsync(assetMovement, cancellationToken);
        }

        public async Task<IReadOnlyCollection<AssetMovement>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
        {
            return await _appDbContext.AssetMovement
                .AsNoTracking()
                .Where(movement => movement.AssetId == assetId)
                .OrderBy(movement => movement.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<AssetMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _appDbContext.AssetMovement
                .AsNoTracking()
                .FirstOrDefaultAsync(movement => movement.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<AssetMovement>>GetRecentByAssetIdAsync(Guid assetId, DateTime fromUtc, CancellationToken cancellationToken)
        {
            return await _appDbContext.AssetMovement
                .AsNoTracking()
                .Where(assetMovement => assetMovement.AssetId == assetId && assetMovement.CreatedAtUtc >= fromUtc)
                .OrderByDescending(assetMovement => assetMovement.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _appDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
