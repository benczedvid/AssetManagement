using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories
{
    public sealed class StoreRepository : IStoreRepository
    {
        private readonly AppDbContext _dbContext;

        public StoreRepository(AppDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            _dbContext = dbContext;
        }
        public async Task AddAsync(Store store, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(store);
            await _dbContext.Stores.AddAsync(store, cancellationToken);
        }

        public async Task<IReadOnlyList<Store>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Stores.AsNoTracking().ToListAsync(cancellationToken);
        }

        public Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _dbContext.Stores
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        }
        public async Task<IReadOnlyList<Store>>GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        {
            if(ids.Count == 0)
            {
                return [];
            }

            return await _dbContext.Stores
                .AsNoTracking()
                .Where(store => ids.Contains(store.Id))
                .ToListAsync(cancellationToken);
        }

        public Task<Store?> GetByStoreNumberAsync(string storeNumber, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storeNumber);

            var normalizedStoreNumber = storeNumber.Trim();

            return _dbContext.Stores
                .AsNoTracking()
                .SingleOrDefaultAsync(store => store.StoreNumber == normalizedStoreNumber, cancellationToken);
        }

        public Task<Store?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _dbContext.Stores
                .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
