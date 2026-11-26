using AssetManagement.Application.Common.Interfaces.Vendors;

namespace AssetManagement.Application.Vendors.DetachContactFromVendor
{
    public sealed class DetachContactFromVendorHandler
    {
        private readonly IVendorRepository _vendorRepository;

        public DetachContactFromVendorHandler(IVendorRepository vendorRepository)
        {
            ArgumentNullException.ThrowIfNull(vendorRepository);
            _vendorRepository = vendorRepository;
        }

        public async Task HandleAsync(Guid vendorId, Guid contactId, CancellationToken cancellationToken)
        {
            var vendor = await _vendorRepository.GetForUpdateByIdAsync(vendorId, cancellationToken) ?? throw new VendorNotFoundException(vendorId);

            vendor.DetachContact(contactId);

            await _vendorRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
