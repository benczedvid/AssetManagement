using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Vendors.CreateVendor;
using AssetManagement.Application.Vendors.GetVendorById;
using AssetManagement.Application.Vendors.GetVendors;
using AssetManagement.Application.Vendors.UpdateVendor;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Domain.Entities.Vendors;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetManagement.WebApi.IntegrationTests.Controllers.VendorControllers
{
    [TestClass]
    public sealed class VendorEndpointTests
    {
        private const string Endpoint = "/api/vendors";

        private AssetManagementWebApplicationFactory? _applicationFactory;
        private HttpClient? _httpClient;
        private JsonSerializerOptions _jsonOptions = null!;

        [TestInitialize]
        public void Initialize()
        {
            _applicationFactory = new AssetManagementWebApplicationFactory();
            _httpClient = _applicationFactory.CreateClient();

            _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        [TestCleanup]
        public void Cleanup()
        {
            _httpClient?.Dispose();
            _applicationFactory?.Dispose();
        }

        [TestMethod]
        public async Task CreateVendor_Should_Return_201_And_Persist_Vendor()
        {
            AddAuthenticatedHeaders();

            var request = CreateValidCreateRequest();

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<CreateVendorResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.AreNotEqual(Guid.Empty, responseBody.Id);
            Assert.AreEqual(request.Name, responseBody.Name);
            Assert.AreEqual(request.Webpage, responseBody.Webpage);
            Assert.IsNotNull(response.Headers.Location);
            StringAssert.EndsWith(response.Headers.Location.ToString(), $"{Endpoint}/{responseBody.Id}", StringComparison.OrdinalIgnoreCase);

            var persistedVendor = await FindVendorAsync(responseBody.Id);

            Assert.IsNotNull(persistedVendor);
            Assert.AreEqual(request.Name, persistedVendor.Name);
            Assert.AreEqual(request.Address.City, persistedVendor.Address.City);
        }

        [TestMethod]
        public async Task CreateVendor_Should_Return_409_When_Name_Already_Exists()
        {
            AddAuthenticatedHeaders();

            await SeedVendorsAsync(CreateVendor("Tech Supplier Kft."));

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, CreateValidCreateRequest(), _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateVendor_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, CreateValidCreateRequest(), _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        [DataRow("CentralUser")]
        [DataRow("StoreManagement")]
        public async Task CreateVendor_Should_Return_403_When_User_Cannot_Manage_Vendors(string role)
        {
            AddAuthenticatedHeaders(role);

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, CreateValidCreateRequest(), _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateVendor_Should_Allow_Hotline_Agent()
        {
            AddAuthenticatedHeaders("HotlineAgent");

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, CreateValidCreateRequest(), _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        }

        [TestMethod]
        public async Task GetVendorById_Should_Return_200_With_Vendor()
        {
            AddAuthenticatedHeaders();

            var vendor = CreateVendor("Tech Supplier Kft.");

            vendor.AddContact(
                firstName: "Jane",
                lastName: "Smith",
                jobTitle: "Account Manager",
                emailAddress: "jane.smith@example.com",
                phoneNumber: "+36 30 765 4321");

            await SeedVendorsAsync(vendor);

            using var response = await _httpClient!.GetAsync($"{Endpoint}/{vendor.VendorId}", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<GetVendorByIdResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.AreEqual(vendor.VendorId, responseBody.Id);
            Assert.AreEqual(vendor.Name, responseBody.Name);
            Assert.AreEqual(vendor.Address.PostalCode, responseBody.Address.PostalCode);
            Assert.HasCount(1, responseBody.Contacts);
            Assert.AreEqual("jane.smith@example.com", responseBody.Contacts.Single().EmailAddress);
        }

        [TestMethod]
        public async Task GetVendorById_Should_Return_404_When_Vendor_Does_Not_Exist()
        {
            AddAuthenticatedHeaders();

            using var response = await _httpClient!.GetAsync($"{Endpoint}/{Guid.NewGuid()}", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task GetVendors_Should_Return_200_With_All_Vendors()
        {
            AddAuthenticatedHeaders();

            var firstVendor = CreateVendor("Tech Supplier Kft.");
            var secondVendor = CreateVendor("Service Partner Zrt.");

            await SeedVendorsAsync(firstVendor, secondVendor);

            using var response = await _httpClient!.GetAsync(Endpoint, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<IReadOnlyList<GetVendorsResponse>>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.HasCount(2, responseBody);
            Assert.AreEqual(firstVendor.Name, responseBody.Single(vendor => vendor.Id == firstVendor.VendorId).Name);
            Assert.AreEqual(secondVendor.Name, responseBody.Single(vendor => vendor.Id == secondVendor.VendorId).Name);
        }

        [TestMethod]
        public async Task GetVendors_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            using var response = await _httpClient!.GetAsync(Endpoint, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task UpdateVendor_Should_Return_200_And_Persist_Changes()
        {
            AddAuthenticatedHeaders();

            var vendor = CreateVendor("Tech Supplier Kft.");

            await SeedVendorsAsync(vendor);

            var request = new UpdateVendorRequest(
                Name: "Tech Supplier Zrt.",
                Address: new AddressRequest(
                    CountryCode: CountryCodes.HUN,
                    PostalCode: "1138",
                    City: "Budapest",
                    Street: "Váci",
                    PublicSpace: PublicSpaces.Road,
                    HouseNumber: "1"),
                Webpage: "https://new.example.com",
                Contacts: []);

            using var response = await _httpClient!.PutAsJsonAsync($"{Endpoint}/{vendor.VendorId}", request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<UpdateVendorResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.AreEqual(vendor.VendorId, responseBody.Id);
            Assert.AreEqual("Tech Supplier Zrt.", responseBody.Name);

            var persistedVendor = await FindVendorAsync(vendor.VendorId);

            Assert.IsNotNull(persistedVendor);
            Assert.AreEqual("Tech Supplier Zrt.", persistedVendor.Name);
            Assert.AreEqual("1138", persistedVendor.Address.PostalCode);
            Assert.AreEqual(PublicSpaces.Road, persistedVendor.Address.PublicSpace);
            Assert.AreEqual("https://new.example.com", persistedVendor.Webpage);
        }

        [TestMethod]
        public async Task UpdateVendor_Should_Return_404_When_Vendor_Does_Not_Exist()
        {
            AddAuthenticatedHeaders();

            var request = new UpdateVendorRequest(
                Name: "Tech Supplier Zrt.",
                Address: CreateValidCreateRequest().Address,
                Webpage: null,
                Contacts: []);

            using var response = await _httpClient!.PutAsJsonAsync($"{Endpoint}/{Guid.NewGuid()}", request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static CreateVendorRequest CreateValidCreateRequest()
        {
            return new CreateVendorRequest(
                Name: "Tech Supplier Kft.",
                Address: new AddressRequest(
                    CountryCode: CountryCodes.HUN,
                    PostalCode: "1117",
                    City: "Budapest",
                    Street: "Budafoki",
                    PublicSpace: PublicSpaces.Street,
                    HouseNumber: "56"),
                Webpage: "https://example.com");
        }

        private static Vendor CreateVendor(string name)
        {
            return Vendor.Create(
                name: name,
                countryCode: CountryCodes.HUN,
                postalCode: "1117",
                city: "Budapest",
                street: "Budafoki",
                publicSpace: PublicSpaces.Street,
                houseNumber: "56",
                webpage: "https://example.com");
        }

        private void AddAuthenticatedHeaders(string? role = null)
        {
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeader, "true");
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraObjectIdHeader, Guid.NewGuid().ToString());
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraTenantIdHeader, Guid.NewGuid().ToString());

            if (role is not null)
            {
                _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.ApplicationRoleHeader, role);
            }
        }

        private async Task SeedVendorsAsync(params Vendor[] vendors)
        {
            using var scope = _applicationFactory!.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Vendors.AddRangeAsync(vendors, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        private async Task<Vendor?> FindVendorAsync(Guid vendorId)
        {
            using var scope = _applicationFactory!.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await dbContext.Vendors
                .AsNoTracking()
                .SingleOrDefaultAsync(vendor => vendor.VendorId == vendorId, CancellationToken.None);
        }
    }
}
