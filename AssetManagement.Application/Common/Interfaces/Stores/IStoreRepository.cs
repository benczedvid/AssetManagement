using AssetManagement.Domain.Entities.Stores;

namespace AssetManagement.Application.Common.Interfaces.Stores
{
    public interface IStoreRepository
    {
        Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Store>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken= default);
        Task<Store?>GetByStoreNumberAsync(string storeNumber, CancellationToken cancellationToken= default);
        Task<Store?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(Store store, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task <IReadOnlyList<Store>>GetAllAsync(CancellationToken cancellationToken = default);
    }
}
