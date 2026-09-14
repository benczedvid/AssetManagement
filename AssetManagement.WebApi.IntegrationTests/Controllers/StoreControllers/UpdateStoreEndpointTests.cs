
using AssetManagement.Application.Stores.UpdateStore;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetManagement.WebApi.IntegrationTests.Controllers.StoreControllers
{
    [TestClass]
    public sealed class UpdateStoreEndpointTests
    {
        private DeathStarWebApplicationFactory? _applicationFactory = null!;
        private HttpClient? _httpClient = null!;
        private JsonSerializerOptions _jsonOptions = null!;

        [TestInitialize]
        public void Initialize()
        {
            _applicationFactory = new DeathStarWebApplicationFactory();
            _httpClient = _applicationFactory.CreateClient();
            _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        [TestCleanup]
        public void Cleanup()
        {
            _applicationFactory?.Dispose();
            _httpClient?.Dispose();
        }
        [TestMethod]
        public async Task UpdateStore_Should_Return_200_When_Request_Is_Valid()
        {
            AddAuthenticatedHeaders();
            var store = Store.Create(
                storeNumber: "321",
                name: "Budapest M3",
                countryCode: CountryCodes.HUN,
                postalCode: "1152",
                city: "Budapest",
                address: "Városkapu",
                publicSpace: PublicSpaces.utca,
                houseNumber: "5"
                );
            await SeedStoreAsync(store);

            var updateStore = new UpdateStoreRequest(
                Name: "Pécs",
                CountryCode: CountryCodes.HUN,
                PostalCode: "7634",
                City: "Pécs",
                Address: "Makay István",
                PublicSpace: PublicSpaces.út,
                HouseNumber: "11");
            var response = await _httpClient!.PutAsJsonAsync($"/api/stores/{store.Id}", updateStore, CancellationToken.None);
            var responseBody = await response.Content.ReadFromJsonAsync<UpdateStoreResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(store.Id, responseBody.Id);
            Assert.AreEqual(store.StoreNumber, responseBody.StoreNumber);
            Assert.AreEqual(updateStore.Name, responseBody.Name);
            Assert.AreEqual(updateStore.City, responseBody.City);
            Assert.AreEqual(updateStore.Address, responseBody.Address);
            Assert.AreEqual(updateStore.PublicSpace, responseBody.PublicSpace);
            Assert.AreEqual(updateStore.HouseNumber, responseBody.HouseNumber);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_404_When_Store_Does_Not_Exist()
        {
            AddAuthenticatedHeaders();

            Guid invalidId = Guid.NewGuid();

            var updateStore = new UpdateStoreRequest(
                Name: "Pécs",
                CountryCode: CountryCodes.HUN,
                PostalCode: "7634",
                City: "Pécs",
                Address: "Makay István",
                PublicSpace: PublicSpaces.út,
                HouseNumber: "11");

            var response = await _httpClient!.PutAsJsonAsync($"/api/stores/{invalidId}", updateStore, CancellationToken.None);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_400_When_Request_Body_Is_Null()
        {
            AddAuthenticatedHeaders();

            var store = Store.Create(
                storeNumber: "321",
                name: "Budapest M3",
                countryCode: CountryCodes.HUN,
                postalCode: "1152",
                city: "Budapest",
                address: "Városkapu",
                publicSpace: PublicSpaces.utca,
                houseNumber: "5");
            await SeedStoreAsync(store);
            using var content = new StringContent("null", Encoding.UTF8, "application/json");

            var response = await _httpClient!.PutAsJsonAsync($"/api/stores/{store.Id}", content, CancellationToken.None);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [TestMethod]
        public async Task UpdateStore_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            var request = new UpdateStoreRequest(
                Name: "Pécs",
                CountryCode: CountryCodes.HUN,
                PostalCode: "7634",
                City: "Pécs",
                Address: "Makay István",
                PublicSpace: PublicSpaces.út,
                HouseNumber: "11");

            using var response = await _httpClient!.PutAsJsonAsync($"/api/stores/{Guid.NewGuid()}", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        private void AddAuthenticatedHeaders()
        {
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeader, "true");
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraObjectIdHeader, Guid.NewGuid().ToString());
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraTenantIdHeader, Guid.NewGuid().ToString());
        }

        private async Task SeedStoreAsync(Store store)
        {
            using var scope = _applicationFactory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Stores.AddAsync(store, CancellationToken.None);

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }
}