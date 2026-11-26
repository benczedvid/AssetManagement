using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Infrastructure.Persistence.Repositories;
using AssetManagement.Tests.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Tests.Infrastructure.Persistence.Repositories
{
    [TestClass]
    public sealed class ContactRepositoryTests
    {
        private SqliteConnection _sqliteConnection = null!;
        private DbContextOptions<AppDbContext> _options = null!;
        private AppDbContext _appDbContext = null!;
        private ContactRepository _contactRepository = null!;

        [TestInitialize]
        public void Initialize()
        {
            _sqliteConnection = new SqliteConnection("Data Source=:memory:");
            _sqliteConnection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_sqliteConnection).Options;

            _appDbContext = new AppDbContext(_options);
            _appDbContext.Database.EnsureCreated();
            _contactRepository = new ContactRepository(_appDbContext);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _appDbContext?.Dispose();
            _sqliteConnection?.Dispose();
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_DbContext_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new ContactRepository(null!));

            Assert.AreEqual("appDbContext", exception.ParamName);
        }

        [TestMethod]
        public async Task AddAsync_And_SaveChangesAsync_Should_Persist_Standalone_Contact()
        {
            var contact = new ContactBuilder().Build();

            await _contactRepository.AddAsync(contact, CancellationToken.None);
            await _contactRepository.SaveChangesAsync(CancellationToken.None);

            await using var verificationContext = new AppDbContext(_options);
            var persistedContact = await verificationContext.Contacts.SingleAsync(candidate => candidate.ContactId == contact.ContactId);

            Assert.IsNull(persistedContact.VendorId);
            Assert.AreEqual(contact.PhoneNumber, persistedContact.PhoneNumber);
        }

        [TestMethod]
        public async Task GetByIdAsync_Should_Return_Null_When_Contact_Does_Not_Exist()
        {
            var contact = await _contactRepository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

            Assert.IsNull(contact);
        }

        [TestMethod]
        public async Task GetAllAsync_Should_Return_Contacts_Ordered_By_Name()
        {
            _appDbContext.Contacts.AddRange(
                new ContactBuilder().WithLastName("Zöld").Build(),
                new ContactBuilder().WithLastName("Albert").Build());
            await _appDbContext.SaveChangesAsync();

            var contacts = await _contactRepository.GetAllAsync(CancellationToken.None);

            Assert.HasCount(2, contacts);
            Assert.AreEqual("Albert", contacts[0].LastName);
        }

        [TestMethod]
        public async Task Attach_And_Detach_Should_Persist_VendorId()
        {
            var vendor = new VendorBuilder().Build();
            var contact = new ContactBuilder().Build();
            _appDbContext.Vendors.Add(vendor);
            _appDbContext.Contacts.Add(contact);
            await _appDbContext.SaveChangesAsync();

            await using (var attachContext = new AppDbContext(_options))
            {
                var vendorRepository = new VendorRepository(attachContext);
                var contactRepository = new ContactRepository(attachContext);
                var trackedVendor = await vendorRepository.GetForUpdateByIdAsync(vendor.VendorId);
                var trackedContact = await contactRepository.GetForUpdateByIdAsync(contact.ContactId);

                trackedVendor!.AttachContact(trackedContact!);
                await vendorRepository.SaveChangesAsync();
            }

            await using (var verificationContext = new AppDbContext(_options))
            {
                var loadedVendor = await new VendorRepository(verificationContext).GetByIdAsync(vendor.VendorId);

                Assert.AreEqual(contact.ContactId, loadedVendor!.Contacts.Single().ContactId);
            }

            await using (var detachContext = new AppDbContext(_options))
            {
                var vendorRepository = new VendorRepository(detachContext);
                var trackedVendor = await vendorRepository.GetForUpdateByIdAsync(vendor.VendorId);

                trackedVendor!.DetachContact(contact.ContactId);
                await vendorRepository.SaveChangesAsync();
            }

            await using (var verificationContext = new AppDbContext(_options))
            {
                var persistedContact = await verificationContext.Contacts.SingleAsync(candidate => candidate.ContactId == contact.ContactId);
                var loadedVendor = await new VendorRepository(verificationContext).GetByIdAsync(vendor.VendorId);

                Assert.IsNull(persistedContact.VendorId);
                Assert.IsEmpty(loadedVendor!.Contacts);
            }
        }

        [TestMethod]
        public async Task Remove_Should_Delete_Attached_Contact()
        {
            var vendor = new VendorBuilder().Build();
            var contact = new ContactBuilder().Build();
            vendor.AttachContact(contact);
            _appDbContext.Vendors.Add(vendor);
            await _appDbContext.SaveChangesAsync();

            var trackedContact = await _contactRepository.GetForUpdateByIdAsync(contact.ContactId);
            _contactRepository.Remove(trackedContact!);
            await _contactRepository.SaveChangesAsync();

            await using var verificationContext = new AppDbContext(_options);

            Assert.IsFalse(await verificationContext.Contacts.AnyAsync());
            Assert.IsTrue(await verificationContext.Vendors.AnyAsync(candidate => candidate.VendorId == vendor.VendorId));
        }
    }
}
