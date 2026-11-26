using AssetManagement.Application.Common.Interfaces.Contacts;
using AssetManagement.Domain.Entities.Contacts;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories
{
    public sealed class ContactRepository : IContactRepository
    {
        private readonly AppDbContext _appDbContext;

        public ContactRepository(AppDbContext appDbContext)
        {
            ArgumentNullException.ThrowIfNull(appDbContext);

            _appDbContext = appDbContext;
        }

        public async Task AddAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(contact);

            await _appDbContext.Contacts.AddAsync(contact, cancellationToken);
        }

        public Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _appDbContext.Contacts.AsNoTracking().SingleOrDefaultAsync(contact => contact.ContactId == id, cancellationToken);
        }

        public Task<Contact?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _appDbContext.Contacts.SingleOrDefaultAsync(contact => contact.ContactId == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _appDbContext.Contacts
                .AsNoTracking()
                .OrderBy(contact => contact.LastName)
                .ThenBy(contact => contact.FirstName)
                .ToListAsync(cancellationToken);
        }

        public void Remove(Contact contact)
        {
            ArgumentNullException.ThrowIfNull(contact);

            _appDbContext.Contacts.Remove(contact);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _appDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
