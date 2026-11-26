using AssetManagement.Application.Vendors;
using AssetManagement.Application.Vendors.GetVendorById;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Vendors.GetVendorById
{
    [TestClass]
    public sealed class GetVendorByIdHandlerTests
    {
        private FakeVendorRepository _repository = null!;
        private GetVendorByIdHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _repository = new FakeVendorRepository();
            _handler = new GetVendorByIdHandler(_repository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new GetVendorByIdHandler(null!));

            Assert.AreEqual("vendorRepository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Vendor_When_It_Exists()
        {
            var vendor = new VendorBuilder()
                .WithName("Tech Supplier Kft.")
                .WithCountryCode(CountryCodes.HUN)
                .WithPostalCode("1117")
                .WithCity("Budapest")
                .WithStreet("Budafoki")
                .WithPublicSpace(PublicSpaces.Street)
                .WithHouseNumber("56")
                .WithWebpage("https://example.com")
                .Build();

            _repository.Seed(vendor);

            var response = await _handler.HandleAsync(vendor.VendorId, CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(vendor.VendorId, response.Id);
            Assert.AreEqual("Tech Supplier Kft.", response.Name);
            Assert.AreEqual(CountryCodes.HUN, response.Address.CountryCode);
            Assert.AreEqual("1117", response.Address.PostalCode);
            Assert.AreEqual("Budapest", response.Address.City);
            Assert.AreEqual("Budafoki", response.Address.Street);
            Assert.AreEqual(PublicSpaces.Street, response.Address.PublicSpace);
            Assert.AreEqual("56", response.Address.HouseNumber);
            Assert.AreEqual("https://example.com", response.Webpage);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Empty_Contacts_When_Vendor_Has_No_Contacts()
        {
            var vendor = new VendorBuilder().Build();

            _repository.Seed(vendor);

            var response = await _handler.HandleAsync(vendor.VendorId, CancellationToken.None);

            Assert.IsEmpty(response.Contacts);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Map_Vendor_Contacts()
        {
            var vendor = new VendorBuilder().Build();

            var contact = new ContactBuilder().Build();
            var contactId = contact.ContactId;

            vendor.AttachContact(contact);

            _repository.Seed(vendor);

            var response = await _handler.HandleAsync(vendor.VendorId, CancellationToken.None);

            Assert.HasCount(1, response.Contacts);

            var contactResponse = response.Contacts.Single();

            Assert.AreEqual(contactId, contactResponse.Id);
            Assert.AreEqual("Jane", contactResponse.FirstName);
            Assert.AreEqual("Smith", contactResponse.LastName);
            Assert.AreEqual("Account Manager", contactResponse.JobTitle);
            Assert.AreEqual("+36 30 765 4321", contactResponse.PhoneNumber);
            Assert.AreEqual("jane.smith@example.com", contactResponse.EmailAddress);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Vendor_Does_Not_Exist()
        {
            var vendorId = Guid.NewGuid();

            var exception = await Assert.ThrowsExactlyAsync<VendorNotFoundException>(
                () => _handler.HandleAsync(vendorId, CancellationToken.None));

            Assert.AreEqual(vendorId, exception.Id);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Request_Vendor_By_Id()
        {
            var vendor = new VendorBuilder().Build();

            _repository.Seed(vendor);

            await _handler.HandleAsync(vendor.VendorId, CancellationToken.None);

            Assert.AreEqual(1, _repository.GetByIdCallCount);
            Assert.AreEqual(vendor.VendorId, _repository.LastRequestedVendorId);
            Assert.AreEqual(0, _repository.GetForUpdateByIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Repository()
        {
            var vendor = new VendorBuilder().Build();

            _repository.Seed(vendor);

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(vendor.VendorId, cancellationToken);

            Assert.AreEqual(cancellationToken, _repository.LastGetByIdCancellationToken);
        }
    }
}
