using AssetManagement.Application.Common.Interfaces.Contacts;

namespace AssetManagement.Application.Contacts.GetContactById
{
    public sealed class GetContactByIdHandler
    {
        private readonly IContactRepository _contactRepository;

        public GetContactByIdHandler(IContactRepository contactRepository)
        {
            ArgumentNullException.ThrowIfNull(contactRepository);
            _contactRepository = contactRepository;
        }

        public async Task<ContactDetailsResponse> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var contact = await _contactRepository.GetByIdAsync(id, cancellationToken) ?? throw new ContactNotFoundException(id);

            return ContactMapper.ToDetailsResponse(contact);
        }
    }
}
