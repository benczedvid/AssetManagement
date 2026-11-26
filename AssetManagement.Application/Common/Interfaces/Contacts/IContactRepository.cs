using AssetManagement.Domain.Entities.Contacts;

namespace AssetManagement.Application.Common.Interfaces.Contacts
{
    public interface IContactRepository
    {
        Task AddAsync(Contact contact, CancellationToken cancellationToken = default);
        Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Contact?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default);
        void Remove(Contact contact);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
