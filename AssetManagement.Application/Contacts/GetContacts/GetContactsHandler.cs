using AssetManagement.Application.Common.Interfaces.Contacts;

namespace AssetManagement.Application.Contacts.GetContacts
{
    public sealed class GetContactsHandler
    {
        private readonly IContactRepository _contactRepository;

        public GetContactsHandler(IContactRepository contactRepository)
        {
            ArgumentNullException.ThrowIfNull(contactRepository);
            _contactRepository = contactRepository;
        }

        public async Task<IReadOnlyList<ContactDetailsResponse>> HandleAsync(CancellationToken cancellationToken)
        {
            var contacts = await _contactRepository.GetAllAsync(cancellationToken);

            return [.. contacts.Select(ContactMapper.ToDetailsResponse)];
        }
    }
}
