using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Common.Interfaces.Assets
{
    public interface IAssetRepository
    {
        Task AddAsync(Asset asset, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Asset?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
        Task<Asset?> GetByRfidTagIdAsync(string rfidTagId, CancellationToken cancellationToken = default);
        Task<Asset?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Asset?> GetForUpdateBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Asset>> GetByAssignedUserIdAsync(Guid assignedUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Asset>> GetAllByStoreIdAsync(Guid storeId,CancellationToken cancellationToken = default);
        Task<Asset?> GetByIdAndStoreIdAsync(Guid id, Guid storeId, CancellationToken cancellationToken = default);
    }
}