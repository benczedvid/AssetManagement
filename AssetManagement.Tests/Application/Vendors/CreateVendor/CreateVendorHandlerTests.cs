using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Vendors;
using AssetManagement.Application.Vendors.CreateVendor;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Vendors.CreateVendor
{
    [TestClass]
    public sealed class CreateVendorHandlerTests
    {
        private FakeVendorRepository _repository = null!;
        private CreateVendorHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _repository = new FakeVendorRepository();
            _handler = new CreateVendorHandler(_repository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new CreateVendorHandler(null!));

            Assert.AreEqual("vendorRepository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Request_Is_Null()
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => _handler.HandleAsync(null!, CancellationToken.None));

            Assert.AreEqual("request", exception.ParamName);
            Assert.AreEqual(0, _repository.GetByNameCallCount);
            Assert.AreEqual(0, _repository.AddCallCount);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Create_And_Save_Vendor()
        {
            var request = CreateValidRequest();

            await _handler.HandleAsync(request, CancellationToken.None);

            Assert.AreEqual(1, _repository.AddCallCount);
            Assert.AreEqual(1, _repository.SaveChangesCallCount);
            Assert.HasCount(1, _repository.Vendors);

            var vendor = _repository.Vendors.Single();

            Assert.AreEqual(request.Name, vendor.Name);
            Assert.AreEqual(request.Address.CountryCode, vendor.Address.CountryCode);
            Assert.AreEqual(request.Address.PostalCode, vendor.Address.PostalCode);
            Assert.AreEqual(request.Address.City, vendor.Address.City);
            Assert.AreEqual(request.Address.Street, vendor.Address.Street);
            Assert.AreEqual(request.Address.PublicSpace, vendor.Address.PublicSpace);
            Assert.AreEqual(request.Address.HouseNumber, vendor.Address.HouseNumber);
            Assert.AreEqual(request.Webpage, vendor.Webpage);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Created_Vendor()
        {
            var request = CreateValidRequest();

            var response = await _handler.HandleAsync(request, CancellationToken.None);

            var vendor = _repository.Vendors.Single();

            Assert.IsNotNull(response);
            Assert.AreEqual(vendor.VendorId, response.Id);
            Assert.AreNotEqual(Guid.Empty, response.Id);
            Assert.AreEqual(request.Name, response.Name);
            Assert.IsNotNull(response.Address);
            Assert.AreEqual(request.Address.CountryCode, response.Address.CountryCode);
            Assert.AreEqual(request.Address.PostalCode, response.Address.PostalCode);
            Assert.AreEqual(request.Address.City, response.Address.City);
            Assert.AreEqual(request.Address.Street, response.Address.Street);
            Assert.AreEqual(request.Address.PublicSpace, response.Address.PublicSpace);
            Assert.AreEqual(request.Address.HouseNumber, response.Address.HouseNumber);
            Assert.AreEqual(request.Webpage, response.Webpage);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Trim_Vendor_Name()
        {
            var request = CreateValidRequest() with { Name = "  Tech Supplier Kft.  " };

            var response = await _handler.HandleAsync(request, CancellationToken.None);

            Assert.AreEqual("Tech Supplier Kft.", response.Name);
            Assert.AreEqual("Tech Supplier Kft.", _repository.Vendors.Single().Name);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Allow_Missing_Webpage()
        {
            var request = CreateValidRequest() with { Webpage = null };

            var response = await _handler.HandleAsync(request, CancellationToken.None);

            Assert.IsNull(response.Webpage);
            Assert.IsNull(_repository.Vendors.Single().Webpage);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Vendor_Name_Already_Exists()
        {
            var existingVendor = new VendorBuilder()
                .WithName("Tech Supplier Kft.")
                .Build();

            _repository.Seed(existingVendor);

            var request = CreateValidRequest();

            var exception = await Assert.ThrowsExactlyAsync<VendorAlreadyExistsException>(
                () => _handler.HandleAsync(request, CancellationToken.None));

            Assert.AreEqual(request.Name, exception.Name);
            Assert.AreEqual(0, _repository.AddCallCount);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
            Assert.HasCount(1, _repository.Vendors);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Look_Up_Vendor_By_Requested_Name()
        {
            var request = CreateValidRequest();

            await _handler.HandleAsync(request, CancellationToken.None);

            Assert.AreEqual(1, _repository.GetByNameCallCount);
            Assert.AreEqual(request.Name, _repository.LastRequestedName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Not_Save_When_Address_Is_Invalid()
        {
            var request = CreateValidRequest() with
            {
                Address = CreateValidAddress() with { City = " " }
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.HandleAsync(request, CancellationToken.None));

            Assert.AreEqual(0, _repository.AddCallCount);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Repository()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(CreateValidRequest(), cancellationToken);

            Assert.AreEqual(cancellationToken, _repository.LastGetByNameCancellationToken);
            Assert.AreEqual(cancellationToken, _repository.LastAddCancellationToken);
            Assert.AreEqual(cancellationToken, _repository.LastSaveChangesCancellationToken);
        }

        private static CreateVendorRequest CreateValidRequest()
        {
            return new CreateVendorRequest(
                Name: "Tech Supplier Kft.",
                Address: CreateValidAddress(),
                Webpage: "https://example.com");
        }

        private static AddressRequest CreateValidAddress()
        {
            return new AddressRequest(
                CountryCode: CountryCodes.HUN,
                PostalCode: "1117",
                City: "Budapest",
                Street: "Budafoki",
                PublicSpace: PublicSpaces.Street,
                HouseNumber: "56");
        }
    }
}
