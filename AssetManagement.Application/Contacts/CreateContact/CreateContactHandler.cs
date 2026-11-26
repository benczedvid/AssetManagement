using AssetManagement.Application.Common.Interfaces.Contacts;
using AssetManagement.Domain.Entities.Contacts;

namespace AssetManagement.Application.Contacts.CreateContact
{
    public sealed class CreateContactHandler
    {
        private readonly IContactRepository _contactRepository;

        public CreateContactHandler(IContactRepository contactRepository)
        {
            ArgumentNullException.ThrowIfNull(contactRepository);
            _contactRepository = contactRepository;
        }

        public async Task<ContactDetailsResponse> HandleAsync(CreateContactRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var contact = Contact.Create(
                firstName: request.FirstName,
                lastName: request.LastName,
                jobTitle: request.JobTitle,
                emailAddress: request.EmailAddress,
                phoneNumber: request.PhoneNumber);

            await _contactRepository.AddAsync(contact, cancellationToken);
            await _contactRepository.SaveChangesAsync(cancellationToken);

            return ContactMapper.ToDetailsResponse(contact);
        }
    }
}
