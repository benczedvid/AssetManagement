using AssetManagement.Application.Common.Interfaces.Contacts;
using AssetManagement.Domain.Entities.Contacts;

namespace AssetManagement.Tests.Fakes
{
    public sealed class FakeContactRepository : IContactRepository
    {
        private readonly List<Contact> _contacts = [];
        public IReadOnlyList<Contact> Contacts => _contacts.AsReadOnly();
        public int AddCallCount { get; private set; }
        public int GetByIdCallCount { get; private set; }
        public int GetForUpdateByIdCallCount { get; private set; }
        public int GetAllCallCount { get; private set; }
        public int RemoveCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public CancellationToken LastAddCancellationToken { get; private set; }
        public CancellationToken LastGetByIdCancellationToken { get; private set; }
        public CancellationToken LastGetForUpdateByIdCancellationToken { get; private set; }
        public CancellationToken LastGetAllCancellationToken { get; private set; }
        public CancellationToken LastSaveChangesCancellationToken { get; private set; }

        public Task AddAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(contact);

            AddCallCount++;
            LastAddCancellationToken = cancellationToken;

            _contacts.Add(contact);

            return Task.CompletedTask;
        }

        public Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            LastGetByIdCancellationToken = cancellationToken;

            return Task.FromResult(_contacts.SingleOrDefault(contact => contact.ContactId == id));
        }

        public Task<Contact?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetForUpdateByIdCallCount++;
            LastGetForUpdateByIdCancellationToken = cancellationToken;

            return Task.FromResult(_contacts.SingleOrDefault(contact => contact.ContactId == id));
        }

        public Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            GetAllCallCount++;
            LastGetAllCancellationToken = cancellationToken;

            IReadOnlyList<Contact> contacts = [.. _contacts];

            return Task.FromResult(contacts);
        }

        public void Remove(Contact contact)
        {
            ArgumentNullException.ThrowIfNull(contact);

            RemoveCallCount++;

            _contacts.Remove(contact);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            LastSaveChangesCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }

        public void Seed(params Contact[] contacts)
        {
            ArgumentNullException.ThrowIfNull(contacts);

            _contacts.AddRange(contacts);
        }
    }
}
