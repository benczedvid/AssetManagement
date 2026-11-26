using AssetManagement.Application.Common.Interfaces.Contacts;
using AssetManagement.Application.Common.Interfaces.Vendors;
using AssetManagement.Application.Contacts;

namespace AssetManagement.Application.Vendors.AttachContactToVendor
{
    public sealed class AttachContactToVendorHandler
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly IContactRepository _contactRepository;

        public AttachContactToVendorHandler(IVendorRepository vendorRepository, IContactRepository contactRepository)
        {
            ArgumentNullException.ThrowIfNull(vendorRepository);
            ArgumentNullException.ThrowIfNull(contactRepository);
            _vendorRepository = vendorRepository;
            _contactRepository = contactRepository;
        }

        public async Task HandleAsync(Guid vendorId, Guid contactId, CancellationToken cancellationToken)
        {
            var vendor = await _vendorRepository.GetForUpdateByIdAsync(vendorId, cancellationToken) ?? throw new VendorNotFoundException(vendorId);
            var contact = await _contactRepository.GetForUpdateByIdAsync(contactId, cancellationToken) ?? throw new ContactNotFoundException(contactId);

            vendor.AttachContact(contact);

            await _vendorRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
