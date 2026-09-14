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
        public async Task GetStores_Should_Return_200_With_All_Stores()
        {
            AddAuthenticatedHeaders();
            var firstStore = Store.Create(
                storeNumber: "321",
                name: "Budapest - M3",
                countryCode: CountryCodes.HUN,
                postalCode: "1152",
                city: "Budapest",
                address: "Városkapu",
                publicSpace: PublicSpaces.utca,
                houseNumber: "5"
                );
            await SeedStoreAsync(firstStore);
            var secondStore = Store.Create(
                storeNumber: "322",
                name: "Pécs",
                countryCode: CountryCodes.HUN,
                postalCode: "7634",
                city: "Pécs",
                address: "Makay István",
                publicSpace: PublicSpaces.út,
                houseNumber: "11"
                );
            await SeedStoreAsync(secondStore);

            using var response = await _httpClient!.GetAsync("/api/stores", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<IReadOnlyList<GetStoresResponse>>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.HasCount(2, responseBody);

            var firstStoreResponse = responseBody.Single(store => store.Id == firstStore.Id);
            Assert.AreEqual(firstStore.Id, firstStoreResponse.Id);
            Assert.AreEqual(firstStore.StoreNumber, firstStoreResponse.StoreNumber);
            Assert.AreEqual(firstStore.Name, firstStoreResponse.Name);
            Assert.AreEqual(firstStore.City, firstStoreResponse.City);
            Assert.AreEqual(firstStore.Address, firstStoreResponse.Address);
            Assert.AreEqual(firstStore.PublicSpace, firstStoreResponse.PublicSpace);
            Assert.AreEqual(firstStore.HouseNumber, firstStoreResponse.HouseNumber);
        }

        [TestMethod]
        public async Task GetStores_Should_Return_200_With_Empty_List_When_No_Stores_Exist()
        {
            AddAuthenticatedHeaders();
            using var response = await _httpClient!.GetAsync("/api/stores", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<IReadOnlyList<GetStoresResponse>>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.HasCount(0, responseBody);
        }

        [TestMethod]
        public async Task GetStores_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            using var response = await _httpClient!.GetAsync("/api/stores", CancellationToken.None);

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