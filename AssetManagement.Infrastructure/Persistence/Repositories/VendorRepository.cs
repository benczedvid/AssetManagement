using AssetManagement.Application.Common.Interfaces.Vendors;
using AssetManagement.Domain.Entities.Vendors;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories
{
    internal class VendorRepository : IVendorRepository
    {

        private readonly AppDbContext _appDbContext;

        public VendorRepository(AppDbContext appDbContext)
        {
            ArgumentNullException.ThrowIfNull(appDbContext);

            _appDbContext = appDbContext;
        }
        public async Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(vendor);

            await _appDbContext.Vendors.AddAsync(vendor, cancellationToken);
        }

        public async Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _appDbContext.Vendors.AsNoTracking().ToListAsync(cancellationToken);
        }

        public Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _appDbContext.Vendors.AsNoTracking().Include(vendor => vendor.Contacts).SingleOrDefaultAsync(vendor => vendor.VendorId == id, cancellationToken);
        }
        public Task<Vendor?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return _appDbContext.Vendors.AsNoTracking().SingleOrDefaultAsync(vendor => vendor.Name == name, cancellationToken);
        }

        public Task<Vendor?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _appDbContext.Vendors.Include(vendor => vendor.Contacts).SingleOrDefaultAsync(vendor => vendor.VendorId == id, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _appDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
