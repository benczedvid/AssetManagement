
using AssetManagement.Domain.Entities.AssetHistories;

namespace AssetManagement.Application.Common.Interfaces.AssetHistories
{
    public interface IAssetHistoryRepository
    {
        Task AddRangeAsync(IEnumerable<AssetHistory> assetHistories, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AssetHistory>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);
    }
}
