using AssetManagement.Application.Contacts;
using AssetManagement.Application.Contacts.CreateContact;
using AssetManagement.Application.Contacts.DeleteContact;
using AssetManagement.Application.Contacts.GetContactById;
using AssetManagement.Application.Contacts.GetContacts;
using AssetManagement.Application.Contacts.UpdateContact;
using AssetManagement.Domain.Entities.Vendors.Exceptions;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Contacts
{
    [TestClass]
    public sealed class ContactHandlersTests
    {
        private FakeContactRepository _contactRepository = null!;
        private FakeVendorRepository _vendorRepository = null!;

        [TestInitialize]
        public void Initialize()
        {
            _contactRepository = new FakeContactRepository();
            _vendorRepository = new FakeVendorRepository();
        }

        [TestMethod]
        public void Constructors_Should_Throw_When_Dependencies_Are_Null()
        {
            Assert.AreEqual("contactRepository", Assert.ThrowsExactly<ArgumentNullException>(() => new CreateContactHandler(null!)).ParamName);
            Assert.AreEqual("contactRepository", Assert.ThrowsExactly<ArgumentNullException>(() => new GetContactByIdHandler(null!)).ParamName);
            Assert.AreEqual("contactRepository", Assert.ThrowsExactly<ArgumentNullException>(() => new GetContactsHandler(null!)).ParamName);
            Assert.AreEqual("contactRepository", Assert.ThrowsExactly<ArgumentNullException>(() => new UpdateContactHandler(null!, _vendorRepository)).ParamName);
            Assert.AreEqual("vendorRepository", Assert.ThrowsExactly<ArgumentNullException>(() => new UpdateContactHandler(_contactRepository, null!)).ParamName);
            Assert.AreEqual("contactRepository", Assert.ThrowsExactly<ArgumentNullException>(() => new DeleteContactHandler(null!)).ParamName);
        }

        [TestMethod]
        public async Task CreateContact_Should_Persist_Standalone_Contact()
        {
            var handler = new CreateContactHandler(_contactRepository);
            using var cancellationTokenSource = new CancellationTokenSource();

            var response = await handler.HandleAsync(
                new CreateContactRequest(" John ", "Doe", "Sales Manager", "+36 30 123 4567", "john.doe@example.com"),
                cancellationTokenSource.Token);

            var contact = _contactRepository.Contacts.Single();

            Assert.AreEqual(contact.ContactId, response.Id);
            Assert.AreEqual("John", response.FirstName);
            Assert.AreEqual("+36 30 123 4567", response.PhoneNumber);
            Assert.AreEqual("john.doe@example.com", response.EmailAddress);
            Assert.IsNull(response.VendorId);
            Assert.AreEqual(1, _contactRepository.SaveChangesCallCount);
            Assert.AreEqual(cancellationTokenSource.Token, _contactRepository.LastAddCancellationToken);
            Assert.AreEqual(cancellationTokenSource.Token, _contactRepository.LastSaveChangesCancellationToken);
        }

        [TestMethod]
        public async Task CreateContact_Should_Throw_When_Request_Is_Null()
        {
            var handler = new CreateContactHandler(_contactRepository);

            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => handler.HandleAsync(null!, CancellationToken.None));

            Assert.AreEqual("request", exception.ParamName);
            Assert.AreEqual(0, _contactRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task CreateContact_Should_Not_Save_When_Data_Is_Invalid()
        {
            var handler = new CreateContactHandler(_contactRepository);

            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(new CreateContactRequest(" ", "Doe", "Sales", "+36 30 123 4567", "a@b.hu"), CancellationToken.None));

            Assert.AreEqual(0, _contactRepository.AddCallCount);
            Assert.AreEqual(0, _contactRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task GetContactById_Should_Return_Contact_With_VendorId()
        {
            var vendor = new VendorBuilder().Build();
            var contact = new ContactBuilder().Build();
            vendor.AttachContact(contact);
            _contactRepository.Seed(contact);

            var response = await new GetContactByIdHandler(_contactRepository).HandleAsync(contact.ContactId, CancellationToken.None);

            Assert.AreEqual(contact.ContactId, response.Id);
            Assert.AreEqual(vendor.VendorId, response.VendorId);
            Assert.AreEqual(contact.LastName, response.LastName);
            Assert.AreEqual(contact.JobTitle, response.JobTitle);
        }

        [TestMethod]
        public async Task GetContactById_Should_Throw_When_Contact_Does_Not_Exist()
        {
            var contactId = Guid.NewGuid();

            var exception = await Assert.ThrowsExactlyAsync<ContactNotFoundException>(
                () => new GetContactByIdHandler(_contactRepository).HandleAsync(contactId, CancellationToken.None));

            Assert.AreEqual(contactId, exception.Id);
        }

        [TestMethod]
        public async Task GetContacts_Should_Return_All_Contacts()
        {
            _contactRepository.Seed(new ContactBuilder().Build(), new ContactBuilder().WithFirstName("John").Build());

            var response = await new GetContactsHandler(_contactRepository).HandleAsync(CancellationToken.None);

            Assert.HasCount(2, response);
            Assert.AreEqual(1, _contactRepository.GetAllCallCount);
        }

        [TestMethod]
        public async Task UpdateContact_Should_Update_Standalone_Contact()
        {
            var contact = new ContactBuilder().Build();
            _contactRepository.Seed(contact);

            var response = await new UpdateContactHandler(_contactRepository, _vendorRepository).HandleAsync(
                contact.ContactId,
                new UpdateContactRequest("John", "Doe", "Senior Sales Manager", "+36 30 111 2222", "john.doe@example.com"),
                CancellationToken.None);

            Assert.AreEqual("John", contact.FirstName);
            Assert.AreEqual("Senior Sales Manager", response.JobTitle);
            Assert.AreEqual("+36 30 111 2222", response.PhoneNumber);
            Assert.AreEqual(1, _contactRepository.SaveChangesCallCount);
            Assert.AreEqual(0, _vendorRepository.GetForUpdateByIdCallCount);
        }

        [TestMethod]
        public async Task UpdateContact_Should_Throw_When_Contact_Does_Not_Exist()
        {
            await Assert.ThrowsExactlyAsync<ContactNotFoundException>(
                () => new UpdateContactHandler(_contactRepository, _vendorRepository).HandleAsync(
                    Guid.NewGuid(),
                    new UpdateContactRequest("John", "Doe", "Sales", "+36 30 123 4567", "a@b.hu"),
                    CancellationToken.None));

            Assert.AreEqual(0, _contactRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task UpdateContact_Should_Throw_When_PhoneNumber_Is_Used_By_Other_Contact_Of_Same_Vendor()
        {
            var vendor = new VendorBuilder().Build();
            var firstContact = new ContactBuilder().WithPhoneNumber("+36 30 111 1111").Build();
            var secondContact = new ContactBuilder().WithPhoneNumber("+36 30 222 2222").Build();
            vendor.AttachContact(firstContact);
            vendor.AttachContact(secondContact);
            _vendorRepository.Seed(vendor);
            _contactRepository.Seed(firstContact, secondContact);

            await Assert.ThrowsExactlyAsync<VendorContactConflictException>(
                () => new UpdateContactHandler(_contactRepository, _vendorRepository).HandleAsync(
                    firstContact.ContactId,
                    new UpdateContactRequest("Jane", "Smith", "Account Manager", "+36 30 222 2222", "jane.smith@example.com"),
                    CancellationToken.None));

            Assert.AreEqual(0, _contactRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task UpdateContact_Should_Throw_When_Request_Is_Null()
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => new UpdateContactHandler(_contactRepository, _vendorRepository).HandleAsync(Guid.NewGuid(), null!, CancellationToken.None));

            Assert.AreEqual("request", exception.ParamName);
        }

        [TestMethod]
        public async Task DeleteContact_Should_Remove_Contact()
        {
            var contact = new ContactBuilder().Build();
            _contactRepository.Seed(contact);

            await new DeleteContactHandler(_contactRepository).HandleAsync(contact.ContactId, CancellationToken.None);

            Assert.IsEmpty(_contactRepository.Contacts);
            Assert.AreEqual(1, _contactRepository.RemoveCallCount);
            Assert.AreEqual(1, _contactRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task DeleteContact_Should_Throw_When_Contact_Does_Not_Exist()
        {
            await Assert.ThrowsExactlyAsync<ContactNotFoundException>(
                () => new DeleteContactHandler(_contactRepository).HandleAsync(Guid.NewGuid(), CancellationToken.None));

            Assert.AreEqual(0, _contactRepository.RemoveCallCount);
            Assert.AreEqual(0, _contactRepository.SaveChangesCallCount);
        }
    }
}
