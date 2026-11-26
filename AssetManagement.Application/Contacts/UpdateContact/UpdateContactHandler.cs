using AssetManagement.Application.Common.Interfaces.Contacts;
using AssetManagement.Application.Common.Interfaces.Vendors;

namespace AssetManagement.Application.Contacts.UpdateContact
{
    public sealed class UpdateContactHandler
    {
        private readonly IContactRepository _contactRepository;
        private readonly IVendorRepository _vendorRepository;

        public UpdateContactHandler(IContactRepository contactRepository, IVendorRepository vendorRepository)
        {
            ArgumentNullException.ThrowIfNull(contactRepository);
            ArgumentNullException.ThrowIfNull(vendorRepository);
            _contactRepository = contactRepository;
            _vendorRepository = vendorRepository;
        }

        public async Task<ContactDetailsResponse> HandleAsync(Guid id, UpdateContactRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var contact = await _contactRepository.GetForUpdateByIdAsync(id, cancellationToken) ?? throw new ContactNotFoundException(id);

            contact.Update(
                firstName: request.FirstName,
                lastName: request.LastName,
                jobTitle: request.JobTitle,
                emailAddress: request.EmailAddress,
                phoneNumber: request.PhoneNumber);

            if (contact.VendorId is Guid vendorId)
            {
                var vendor = await _vendorRepository.GetForUpdateByIdAsync(vendorId, cancellationToken);

                vendor?.EnsureContactPhoneNumberIsUnique(contact);
            }

            await _contactRepository.SaveChangesAsync(cancellationToken);

            return ContactMapper.ToDetailsResponse(contact);
        }
    }
}
