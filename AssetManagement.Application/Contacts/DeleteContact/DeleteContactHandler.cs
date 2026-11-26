using AssetManagement.Application.Common.Interfaces.Contacts;

namespace AssetManagement.Application.Contacts.DeleteContact
{
    public sealed class DeleteContactHandler
    {
        private readonly IContactRepository _contactRepository;

        public DeleteContactHandler(IContactRepository contactRepository)
        {
            ArgumentNullException.ThrowIfNull(contactRepository);
            _contactRepository = contactRepository;
        }

        public async Task HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var contact = await _contactRepository.GetForUpdateByIdAsync(id, cancellationToken) ?? throw new ContactNotFoundException(id);

            _contactRepository.Remove(contact);

            await _contactRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
