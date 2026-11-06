using AssetManagement.Application.Stores.GetStores;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetManagement.WebApi.IntegrationTests.Controllers.StoreControllers
{
    [TestClass]
    public sealed class GetStoresEndpointTests
    {
        private const string Endpoint = "/api/stores";

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
        public async Task GetStores_Should_Return_200_With_All_Stores()
        {
            AddAuthenticatedHeaders();

            var firstStore = Store.Create(
                storeNumber: "321",
                name: "Budapest - M3",
                countryCode: CountryCodes.HUN,
                postalCode: "1152",
                city: "Budapest",
                street: "Városkapu",
                publicSpace: PublicSpaces.Street,
                houseNumber: "5");

            var secondStore = Store.Create(
                storeNumber: "322",
                name: "Pécs",
                countryCode: CountryCodes.HUN,
                postalCode: "7634",
                city: "Pécs",
                street: "Makay István",
                publicSpace: PublicSpaces.Road,
                houseNumber: "11");

            await SeedStoresAsync(firstStore, secondStore);

            using var response = await _httpClient!.GetAsync(Endpoint, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<IReadOnlyList<GetStoresResponse>>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.HasCount(2, responseBody);

            var firstStoreResponse = responseBody.Single(store => store.Id == firstStore.Id);

            AssertStoreResponse(firstStore, firstStoreResponse);

            var secondStoreResponse = responseBody.Single(store => store.Id == secondStore.Id);

            AssertStoreResponse(secondStore, secondStoreResponse);
        }

        [TestMethod]
        public async Task GetStores_Should_Return_200_With_Empty_List_When_No_Stores_Exist()
        {
            AddAuthenticatedHeaders();

            using var response = await _httpClient!.GetAsync(Endpoint, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<IReadOnlyList<GetStoresResponse>>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.IsEmpty(responseBody);
        }

        [TestMethod]
        public async Task GetStores_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            using var response = await _httpClient!.GetAsync(Endpoint, CancellationToken.None);
            
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static void AssertStoreResponse(Store expectedStore, GetStoresResponse actualResponse)
        {
            Assert.AreEqual(expectedStore.Id, actualResponse.Id);
            Assert.AreEqual(expectedStore.StoreNumber, actualResponse.StoreNumber);
            Assert.AreEqual(expectedStore.Name, actualResponse.Name);
            Assert.IsNotNull(actualResponse.Address);
            Assert.AreEqual(expectedStore.Address.CountryCode, actualResponse.Address.CountryCode);
            Assert.AreEqual(expectedStore.Address.PostalCode, actualResponse.Address.PostalCode);
            Assert.AreEqual(expectedStore.Address.City, actualResponse.Address.City);
            Assert.AreEqual(expectedStore.Address.Street, actualResponse.Address.Street);
            Assert.AreEqual(expectedStore.Address.PublicSpace, actualResponse.Address.PublicSpace);
            Assert.AreEqual(expectedStore.Address.HouseNumber, actualResponse.Address.HouseNumber);
        }

        private void AddAuthenticatedHeaders()
        {
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeader, "true");
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraObjectIdHeader, Guid.NewGuid().ToString());
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraTenantIdHeader, Guid.NewGuid().ToString());
        }

        private async Task SeedStoresAsync(params Store[] stores)
        {
            using var scope = _applicationFactory!.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Stores.AddRangeAsync(stores, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }
}