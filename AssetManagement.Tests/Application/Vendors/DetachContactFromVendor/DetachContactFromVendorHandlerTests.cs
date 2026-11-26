using AssetManagement.Application.Vendors;
using AssetManagement.Application.Vendors.DetachContactFromVendor;
using AssetManagement.Domain.Entities.Vendors.Exceptions;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Vendors.DetachContactFromVendor
{
    [TestClass]
    public sealed class DetachContactFromVendorHandlerTests
    {
        private FakeVendorRepository _vendorRepository = null!;
        private DetachContactFromVendorHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _vendorRepository = new FakeVendorRepository();
            _handler = new DetachContactFromVendorHandler(_vendorRepository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new DetachContactFromVendorHandler(null!));

            Assert.AreEqual("vendorRepository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Detach_Contact_From_Vendor()
        {
            var vendor = new VendorBuilder().Build();
            var contact = new ContactBuilder().Build();
            vendor.AttachContact(contact);
            _vendorRepository.Seed(vendor);

            await _handler.HandleAsync(vendor.VendorId, contact.ContactId, CancellationToken.None);

            Assert.IsNull(contact.VendorId);
            Assert.AreEqual(0, vendor.Contacts.Count);
            Assert.AreEqual(1, _vendorRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Vendor_Does_Not_Exist()
        {
            await Assert.ThrowsExactlyAsync<VendorNotFoundException>(
                () => _handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));

            Assert.AreEqual(0, _vendorRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Contact_Is_Not_Attached_To_Vendor()
        {
            var vendor = new VendorBuilder().Build();
            _vendorRepository.Seed(vendor);

            await Assert.ThrowsExactlyAsync<VendorContactConflictException>(
                () => _handler.HandleAsync(vendor.VendorId, Guid.NewGuid(), CancellationToken.None));

            Assert.AreEqual(0, _vendorRepository.SaveChangesCallCount);
        }
    }
}
