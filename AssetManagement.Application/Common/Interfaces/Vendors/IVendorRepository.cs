using AssetManagement.Domain.Entities.Vendors;

namespace AssetManagement.Application.Common.Interfaces.Vendors
{
    public interface IVendorRepository
    {
        Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default);
        Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Vendor?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<Vendor?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
