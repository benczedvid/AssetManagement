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
        public async Task GetStoreById_Should_Return_200_When_Store_Exists()
        {
            AddAuthenticatedHeaders();

            var store = CreateStore();

            await SeedStoreAsync(store);

            using var response = await _httpClient!.GetAsync(
                $"{Endpoint}/{store.Id}",
                CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody =  await response.Content.ReadFromJsonAsync<GetStoreByIdResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);

            Assert.AreEqual(store.Id, responseBody.Id);
            Assert.AreEqual(store.StoreNumber, responseBody.StoreNumber);
            Assert.AreEqual(store.Name, responseBody.Name);
            Assert.IsNotNull(responseBody.Address);
            Assert.AreEqual(store.Address.CountryCode, responseBody.Address.CountryCode);
            Assert.AreEqual(store.Address.PostalCode, responseBody.Address.PostalCode);
            Assert.AreEqual(store.Address.City, responseBody.Address.City);
            Assert.AreEqual(store.Address.Street, responseBody.Address.Street);
            Assert.AreEqual(store.Address.PublicSpace, responseBody.Address.PublicSpace);
            Assert.AreEqual(store.Address.HouseNumber, responseBody.Address.HouseNumber);
        }

        [TestMethod]
        public async Task GetStoreById_Should_Return_404_When_Store_Does_Not_Exist()
        {
            AddAuthenticatedHeaders();

            var unknownId = Guid.NewGuid();

            using var response = await _httpClient!.GetAsync(
                $"{Endpoint}/{unknownId}",
                CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("The requested resource was not found.", problemDetails.Title);
            Assert.AreEqual($"{Endpoint}/{unknownId}", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }

        [TestMethod]
        public async Task GetStoreById_Should_Return_400_When_Store_Id_Is_Empty()
        {
            AddAuthenticatedHeaders();

            using var response = await _httpClient!.GetAsync(
                $"{Endpoint}/{Guid.Empty}",
                CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(
                $"{Endpoint}/{Guid.Empty}",
                problemDetails.Instance);

            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }

        [TestMethod]
        public async Task GetStoreById_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            using var response = await _httpClient!.GetAsync(
                $"{Endpoint}/{Guid.NewGuid()}",
                CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static Store CreateStore()
        {
            return Store.Create(
                storeNumber: "321",
                name: "Budapest M3",
                countryCode: CountryCodes.HUN,
                postalCode: "1152",
                city: "Budapest",
                street: "Városkapu",
                publicSpace: PublicSpaces.Street,
                houseNumber: "5");
        }

        private void AddAuthenticatedHeaders()
        {
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeader, "true");
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraObjectIdHeader, Guid.NewGuid().ToString());
            _httpClient.DefaultRequestHeaders.Add( TestAuthenticationHandler.EntraTenantIdHeader,  Guid.NewGuid().ToString());
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