using AssetManagement.Application.Common.Interfaces.Vendors;
using AssetManagement.Domain.Entities.Vendors;

namespace AssetManagement.Tests.Fakes
{
    public sealed class FakeVendorRepository : IVendorRepository
    {
        private readonly List<Vendor> _vendors = [];
        public IReadOnlyList<Vendor> Vendors => _vendors.AsReadOnly();
        public int AddCallCount { get; private set; }
        public int GetByIdCallCount { get; private set; }
        public int GetByNameCallCount { get; private set; }
        public int GetForUpdateByIdCallCount { get; private set; }
        public int GetAllCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public Guid? LastRequestedVendorId { get; private set; }
        public string? LastRequestedName { get; private set; }
        public CancellationToken LastAddCancellationToken { get; private set; }
        public CancellationToken LastGetByIdCancellationToken { get; private set; }
        public CancellationToken LastGetByNameCancellationToken { get; private set; }
        public CancellationToken LastGetForUpdateByIdCancellationToken { get; private set; }
        public CancellationToken LastGetAllCancellationToken { get; private set; }
        public CancellationToken LastSaveChangesCancellationToken { get; private set; }

        public Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(vendor);

            AddCallCount++;
            LastAddCancellationToken = cancellationToken;

            _vendors.Add(vendor);

            return Task.CompletedTask;
        }

        public Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            LastRequestedVendorId = id;
            LastGetByIdCancellationToken = cancellationToken;

            var vendor = _vendors.SingleOrDefault(currentVendor => currentVendor.VendorId == id);

            return Task.FromResult(vendor);
        }

        public Task<Vendor?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            GetByNameCallCount++;
            LastRequestedName = name;
            LastGetByNameCancellationToken = cancellationToken;

            var vendor = _vendors.SingleOrDefault(
                currentVendor =>
                    string.Equals(
                        currentVendor.Name,
                        name,
                        StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(vendor);
        }

        public Task<Vendor?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetForUpdateByIdCallCount++;
            LastRequestedVendorId = id;
            LastGetForUpdateByIdCancellationToken = cancellationToken;

            var vendor = _vendors.SingleOrDefault(currentVendor => currentVendor.VendorId == id);

            return Task.FromResult(vendor);
        }

        public Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            GetAllCallCount++;
            LastGetAllCancellationToken = cancellationToken;

            IReadOnlyList<Vendor> vendors = [.. _vendors];

            return Task.FromResult(vendors);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            LastSaveChangesCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }

        public void Seed(Vendor vendor)
        {
            ArgumentNullException.ThrowIfNull(vendor);

            _vendors.Add(vendor);
        }

        public void Seed(IEnumerable<Vendor> vendors)
        {
            ArgumentNullException.ThrowIfNull(vendors);

            foreach (var vendor in vendors)
            {
                Seed(vendor);
            }
        }
    }
}
