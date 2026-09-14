using AssetManagement.Application.Stores.GetStoreById;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetManagement.WebApi.IntegrationTests.Controllers.StoreControllers
{
    [TestClass]
    public sealed class GetStoreEndpointTests
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
        public async Task GetStoreByID_Should_Return_200_When_Store_Exist()
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

            using var response = await _httpClient!.GetAsync($"/api/stores/{store.Id}", CancellationToken.None);
            var responseBody = await response.Content.ReadFromJsonAsync<GetAssetByIdResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(store.Id, responseBody.Id);
            Assert.AreEqual(store.StoreNumber, responseBody.StoreNumber);
            Assert.AreEqual(store.Name, responseBody.Name);
            Assert.AreEqual(store.City, responseBody.City);
            Assert.AreEqual(store.Address, responseBody.Address);
            Assert.AreEqual(store.PublicSpace, responseBody.PublicSpace);
            Assert.AreEqual(store.HouseNumber, responseBody.HouseNumber);
        }

        [TestMethod]
        public async Task GetStoreById_Should_Return_404_When_Store_Does_Not_Exist()
        {
            AddAuthenticatedHeaders();
            Guid unknownId = Guid.NewGuid();

            using var response = await _httpClient!.GetAsync($"/api/stores/{unknownId}", CancellationToken.None);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken.None);

            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.AreEqual("The requested resource was not found.", problemDetails.Title);
            Assert.AreEqual($"/api/stores/{unknownId}", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }
        [TestMethod]
        public async Task GetStoreById_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            using var response = await _httpClient!.GetAsync($"/api/stores/{Guid.NewGuid()}", CancellationToken.None);
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