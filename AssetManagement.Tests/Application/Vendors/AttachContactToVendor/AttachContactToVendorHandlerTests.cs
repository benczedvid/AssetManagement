using AssetManagement.Application.Contacts;
using AssetManagement.Application.Vendors;
using AssetManagement.Application.Vendors.AttachContactToVendor;
using AssetManagement.Domain.Entities.Vendors.Exceptions;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Vendors.AttachContactToVendor
{
    [TestClass]
    public sealed class AttachContactToVendorHandlerTests
    {
        private FakeVendorRepository _vendorRepository = null!;
        private FakeContactRepository _contactRepository = null!;
        private AttachContactToVendorHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _vendorRepository = new FakeVendorRepository();
            _contactRepository = new FakeContactRepository();
            _handler = new AttachContactToVendorHandler(_vendorRepository, _contactRepository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Dependencies_Are_Null()
        {
            Assert.AreEqual("vendorRepository", Assert.ThrowsExactly<ArgumentNullException>(() => new AttachContactToVendorHandler(null!, _contactRepository)).ParamName);
            Assert.AreEqual("contactRepository", Assert.ThrowsExactly<ArgumentNullException>(() => new AttachContactToVendorHandler(_vendorRepository, null!)).ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Attach_Contact_To_Vendor()
        {
            var vendor = new VendorBuilder().Build();
            var contact = new ContactBuilder().Build();
            _vendorRepository.Seed(vendor);
            _contactRepository.Seed(contact);
            using var cancellationTokenSource = new CancellationTokenSource();

            await _handler.HandleAsync(vendor.VendorId, contact.ContactId, cancellationTokenSource.Token);

            Assert.AreEqual(vendor.VendorId, contact.VendorId);
            Assert.AreSame(contact, vendor.Contacts.Single());
            Assert.AreEqual(1, _vendorRepository.SaveChangesCallCount);
            Assert.AreEqual(cancellationTokenSource.Token, _vendorRepository.LastGetForUpdateByIdCancellationToken);
            Assert.AreEqual(cancellationTokenSource.Token, _contactRepository.LastGetForUpdateByIdCancellationToken);
            Assert.AreEqual(cancellationTokenSource.Token, _vendorRepository.LastSaveChangesCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Vendor_Does_Not_Exist()
        {
            var contact = new ContactBuilder().Build();
            _contactRepository.Seed(contact);
            var vendorId = Guid.NewGuid();

            var exception = await Assert.ThrowsExactlyAsync<VendorNotFoundException>(
                () => _handler.HandleAsync(vendorId, contact.ContactId, CancellationToken.None));

            Assert.AreEqual(vendorId, exception.Id);
            Assert.IsNull(contact.VendorId);
            Assert.AreEqual(0, _vendorRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Contact_Does_Not_Exist()
        {
            var vendor = new VendorBuilder().Build();
            _vendorRepository.Seed(vendor);
            var contactId = Guid.NewGuid();

            var exception = await Assert.ThrowsExactlyAsync<ContactNotFoundException>(
                () => _handler.HandleAsync(vendor.VendorId, contactId, CancellationToken.None));

            Assert.AreEqual(contactId, exception.Id);
            Assert.AreEqual(0, _vendorRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Contact_Is_Attached_To_Another_Vendor()
        {
            var firstVendor = new VendorBuilder().Build();
            var secondVendor = new VendorBuilder().WithName("Other Supplier Kft.").Build();
            var contact = new ContactBuilder().Build();
            firstVendor.AttachContact(contact);
            _vendorRepository.Seed([firstVendor, secondVendor]);
            _contactRepository.Seed(contact);

            await Assert.ThrowsExactlyAsync<VendorContactConflictException>(
                () => _handler.HandleAsync(secondVendor.VendorId, contact.ContactId, CancellationToken.None));

            Assert.AreEqual(firstVendor.VendorId, contact.VendorId);
            Assert.AreEqual(0, _vendorRepository.SaveChangesCallCount);
        }
    }
}
