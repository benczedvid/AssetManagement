using AssetManagement.Domain.Entities.AssetMovements;

namespace AssetManagement.Application.Common.Interfaces.AssetMovements
{
    public interface IAssetMovementRepository
    {
        Task<AssetMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<AssetMovement>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AssetMovement>> GetRecentByAssetIdAsync(Guid assetId, DateTime fromUtc, CancellationToken cancellationToken = default);
        Task AddAsync(AssetMovement assetMovement, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
