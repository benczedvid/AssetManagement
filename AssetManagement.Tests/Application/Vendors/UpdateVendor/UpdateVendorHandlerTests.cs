using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Vendors;
using AssetManagement.Application.Vendors.UpdateVendor;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Vendors.UpdateVendor
{
    [TestClass]
    public sealed class UpdateVendorHandlerTests
    {
        private FakeVendorRepository _repository = null!;
        private UpdateVendorHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _repository = new FakeVendorRepository();
            _handler = new UpdateVendorHandler(_repository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new UpdateVendorHandler(null!));

            Assert.AreEqual("repository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Request_Is_Null()
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => _handler.HandleAsync(Guid.NewGuid(), null!, CancellationToken.None));

            Assert.AreEqual("request", exception.ParamName);
            Assert.AreEqual(0, _repository.GetForUpdateByIdCallCount);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Vendor_Does_Not_Exist()
        {
            var vendorId = Guid.NewGuid();

            var exception = await Assert.ThrowsExactlyAsync<VendorNotFoundException>(
                () => _handler.HandleAsync(vendorId, CreateValidRequest(), CancellationToken.None));

            Assert.AreEqual(vendorId, exception.Id);
            Assert.AreEqual(1, _repository.GetForUpdateByIdCallCount);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Update_Vendor_Details()
        {
            var vendor = new VendorBuilder().Build();

            _repository.Seed(vendor);

            var request = CreateValidRequest();

            await _handler.HandleAsync(vendor.VendorId, request, CancellationToken.None);

            Assert.AreEqual(request.Name, vendor.Name);
            Assert.AreEqual(request.Address.CountryCode, vendor.Address.CountryCode);
            Assert.AreEqual(request.Address.PostalCode, vendor.Address.PostalCode);
            Assert.AreEqual(request.Address.City, vendor.Address.City);
            Assert.AreEqual(request.Address.Street, vendor.Address.Street);
            Assert.AreEqual(request.Address.PublicSpace, vendor.Address.PublicSpace);
            Assert.AreEqual(request.Address.HouseNumber, vendor.Address.HouseNumber);
            Assert.AreEqual(request.Webpage, vendor.Webpage);
            Assert.AreEqual(1, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Updated_Vendor()
        {
            var vendor = new VendorBuilder().Build();

            _repository.Seed(vendor);

            var request = CreateValidRequest();

            var response = await _handler.HandleAsync(vendor.VendorId, request, CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(vendor.VendorId, response.Id);
            Assert.AreEqual(request.Name, response.Name);
            Assert.AreEqual(request.Address.CountryCode, response.Address.CountryCode);
            Assert.AreEqual(request.Address.PostalCode, response.Address.PostalCode);
            Assert.AreEqual(request.Address.City, response.Address.City);
            Assert.AreEqual(request.Address.Street, response.Address.Street);
            Assert.AreEqual(request.Address.PublicSpace, response.Address.PublicSpace);
            Assert.AreEqual(request.Address.HouseNumber, response.Address.HouseNumber);
            Assert.AreEqual(request.Webpage, response.Webpage);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Preserve_Vendor_Id()
        {
            var vendor = new VendorBuilder().Build();
            var originalId = vendor.VendorId;

            _repository.Seed(vendor);

            var response = await _handler.HandleAsync(vendor.VendorId, CreateValidRequest(), CancellationToken.None);

            Assert.AreEqual(originalId, vendor.VendorId);
            Assert.AreEqual(originalId, response.Id);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Trim_Updated_Name_And_Webpage()
        {
            var vendor = new VendorBuilder().Build();

            _repository.Seed(vendor);

            var request = CreateValidRequest() with
            {
                Name = "  Service Partner Zrt.  ",
                Webpage = "  https://partner.example.com  "
            };

            var response = await _handler.HandleAsync(vendor.VendorId, request, CancellationToken.None);

            Assert.AreEqual("Service Partner Zrt.", response.Name);
            Assert.AreEqual("https://partner.example.com", response.Webpage);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Clear_Webpage_When_It_Is_Whitespace()
        {
            var vendor = new VendorBuilder()
                .WithWebpage("https://example.com")
                .Build();

            _repository.Seed(vendor);

            var request = CreateValidRequest() with { Webpage = "   " };

            var response = await _handler.HandleAsync(vendor.VendorId, request, CancellationToken.None);

            Assert.IsNull(vendor.Webpage);
            Assert.IsNull(response.Webpage);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Existing_Contacts()
        {
            var vendor = new VendorBuilder().Build();

            var contactId = vendor.AddContact(
                firstName: "Jane",
                lastName: "Smith",
                jobTitle: "Account Manager",
                emailAddress: "jane.smith@example.com",
                phoneNumber: "+36 30 765 4321");

            _repository.Seed(vendor);

            var response = await _handler.HandleAsync(vendor.VendorId, CreateValidRequest(), CancellationToken.None);

            Assert.HasCount(1, response.Contacts);
            Assert.AreEqual(contactId, response.Contacts.Single().Id);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Not_Save_When_Name_Is_Invalid()
        {
            var vendor = new VendorBuilder()
                .WithName("Tech Supplier Kft.")
                .Build();

            _repository.Seed(vendor);

            var request = CreateValidRequest() with { Name = " " };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.HandleAsync(vendor.VendorId, request, CancellationToken.None));

            Assert.AreEqual("Tech Supplier Kft.", vendor.Name);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Repository()
        {
            var vendor = new VendorBuilder().Build();

            _repository.Seed(vendor);

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(vendor.VendorId, CreateValidRequest(), cancellationToken);

            Assert.AreEqual(cancellationToken, _repository.LastGetForUpdateByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _repository.LastSaveChangesCancellationToken);
        }

        private static UpdateVendorRequest CreateValidRequest()
        {
            return new UpdateVendorRequest(
                Name: "Service Partner Zrt.",
                Address: new AddressRequest(
                    CountryCode: CountryCodes.HUN,
                    PostalCode: "1138",
                    City: "Budapest",
                    Street: "Váci",
                    PublicSpace: PublicSpaces.Road,
                    HouseNumber: "1"),
                Webpage: "https://partner.example.com",
                Contacts: []);
        }
    }
}
