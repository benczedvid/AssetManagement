using AssetManagement.Application.Vendors.GetVendors;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Vendors.GetVendors
{
    [TestClass]
    public sealed class GetVendorsHandlerTests
    {
        private FakeVendorRepository _repository = null!;
        private GetVendorsHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _repository = new FakeVendorRepository();
            _handler = new GetVendorsHandler(_repository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new GetVendorsHandler(null!));

            Assert.AreEqual("repository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Empty_List_When_No_Vendors_Exist()
        {
            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.IsEmpty(response);
            Assert.AreEqual(1, _repository.GetAllCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_All_Vendors()
        {
            var firstVendor = new VendorBuilder()
                .WithName("Tech Supplier Kft.")
                .Build();

            var secondVendor = new VendorBuilder()
                .WithName("Service Partner Zrt.")
                .WithoutWebpage()
                .Build();

            _repository.Seed([firstVendor, secondVendor]);

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.HasCount(2, response);

            var firstResponse = response.Single(vendor => vendor.Id == firstVendor.VendorId);
            var secondResponse = response.Single(vendor => vendor.Id == secondVendor.VendorId);

            Assert.AreEqual("Tech Supplier Kft.", firstResponse.Name);
            Assert.AreEqual(firstVendor.Webpage, firstResponse.Webpage);
            Assert.AreEqual("Service Partner Zrt.", secondResponse.Name);
            Assert.IsNull(secondResponse.Webpage);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Map_Vendor_Address()
        {
            var vendor = new VendorBuilder().Build();

            _repository.Seed(vendor);

            var response = await _handler.HandleAsync(CancellationToken.None);

            var address = response.Single().Address;

            Assert.AreEqual(vendor.Address.CountryCode, address.CountryCode);
            Assert.AreEqual(vendor.Address.PostalCode, address.PostalCode);
            Assert.AreEqual(vendor.Address.City, address.City);
            Assert.AreEqual(vendor.Address.Street, address.Street);
            Assert.AreEqual(vendor.Address.PublicSpace, address.PublicSpace);
            Assert.AreEqual(vendor.Address.HouseNumber, address.HouseNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Repository()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(cancellationToken);

            Assert.AreEqual(cancellationToken, _repository.LastGetAllCancellationToken);
        }
    }
}
