using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Stores.CreateStore;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetManagement.WebApi.IntegrationTests.Controllers.StoreControllers
{
    [TestClass]
    public sealed class CreateStoreEndpointTests
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
        public async Task CreateStore_Should_Return_201_Created()
        {
            AddAuthenticatedHeaders();

            var request = CreateValidRequest();

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<CreateStoreResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);

            Assert.AreNotEqual(Guid.Empty, responseBody.Id);
            Assert.AreEqual(request.StoreNumber, responseBody.StoreNumber);
            Assert.AreEqual(request.Name, responseBody.Name);

            Assert.IsNotNull(responseBody.Address);
            Assert.AreEqual(request.Address.CountryCode, responseBody.Address.CountryCode);
            Assert.AreEqual(request.Address.PostalCode, responseBody.Address.PostalCode);
            Assert.AreEqual(request.Address.City,responseBody.Address.City);
            Assert.AreEqual(request.Address.Street,responseBody.Address.Street);
            Assert.AreEqual(request.Address.PublicSpace, responseBody.Address.PublicSpace);
            Assert.AreEqual(request.Address.HouseNumber, responseBody.Address.HouseNumber);

            await AssertStoreWasPersistedAsync(responseBody);
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            var request = CreateValidRequest();

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_409_When_Store_Number_Already_Exists()
        {
            AddAuthenticatedHeaders();

            var firstRequest = CreateValidRequest();

            using var firstResponse = await _httpClient!.PostAsJsonAsync(Endpoint, firstRequest, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Created, firstResponse.StatusCode);

            var secondRequest = CreateValidRequest(name: "Budapest M3 Duplicate");

            using var secondResponse = await _httpClient!.PostAsJsonAsync(Endpoint, secondRequest, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Conflict, secondResponse.StatusCode);

            var problemDetails = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("A resource conflict occurred.", problemDetails.Title);
            Assert.AreEqual(Endpoint, problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_Store_Number_Is_Empty()
        {
            AddAuthenticatedHeaders();

            var request = CreateValidRequest(storeNumber: string.Empty);
            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            await AssertBadRequestAsync(response);
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_Name_Is_Empty()
        {
            AddAuthenticatedHeaders();

            var request = CreateValidRequest(name: string.Empty);

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            await AssertBadRequestAsync(response);
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_City_Is_Empty()
        {
            AddAuthenticatedHeaders();

            var request = CreateValidRequest(
                address: CreateValidAddress(city: string.Empty));

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            await AssertBadRequestAsync(response);
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_Address_Is_Missing()
        {
            AddAuthenticatedHeaders();

            var request = CreateValidRequest(address: null!);

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            await AssertBadRequestAsync(response);
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_Public_Space_Is_Unknown()
        {
            AddAuthenticatedHeaders();

            var invalidPublicSpace = (PublicSpaces)999;

            var request = CreateValidRequest(
                address: CreateValidAddress(
                    publicSpace: invalidPublicSpace));

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            await AssertBadRequestAsync(response);
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_House_Number_Is_Empty()
        {
            AddAuthenticatedHeaders();

            var request = CreateValidRequest(
                address: CreateValidAddress(
                    houseNumber: string.Empty));

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            await AssertBadRequestAsync(response);
        }

        private static CreateStoreRequest CreateValidRequest(string storeNumber = "321", string name = "Budapest M3", AddressRequest? address = null)
        {
            return new CreateStoreRequest(StoreNumber: storeNumber, Name: name, Address: address ?? CreateValidAddress());
        }

        private static AddressRequest CreateValidAddress(
            CountryCodes countryCode = CountryCodes.HUN,
            string postalCode = "1152",
            string city = "Budapest",
            string street = "Városkapu",
            PublicSpaces publicSpace = PublicSpaces.Street,
            string houseNumber = "5")
        {
            return new AddressRequest(
                CountryCode: countryCode,
                PostalCode: postalCode,
                City: city,
                Street: street,
                PublicSpace: publicSpace,
                HouseNumber: houseNumber);
        }

        private async Task AssertBadRequestAsync(HttpResponseMessage response)
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("The request is invalid.", problemDetails.Title);
            Assert.AreEqual(Endpoint, problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }

        private void AddAuthenticatedHeaders()
        {
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeader, "true");
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraObjectIdHeader, Guid.NewGuid().ToString());
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraTenantIdHeader, Guid.NewGuid().ToString());
        }

        private async Task AssertStoreWasPersistedAsync(CreateStoreResponse response)
        {
            using var scope = _applicationFactory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persistedStore = await dbContext.Stores
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    store => store.Id == response.Id,
                    CancellationToken.None);

            Assert.IsNotNull(persistedStore);
            Assert.AreEqual(response.StoreNumber, persistedStore.StoreNumber);
            Assert.AreEqual(response.Name, persistedStore.Name);
            Assert.AreEqual(response.Address.CountryCode, persistedStore.Address.CountryCode);
            Assert.AreEqual(response.Address.PostalCode, persistedStore.Address.PostalCode);
            Assert.AreEqual(response.Address.City, persistedStore.Address.City);
            Assert.AreEqual(response.Address.Street, persistedStore.Address.Street);
            Assert.AreEqual(response.Address.PublicSpace, persistedStore.Address.PublicSpace);
            Assert.AreEqual(response.Address.HouseNumber, persistedStore.Address.HouseNumber);
        }
    }
}