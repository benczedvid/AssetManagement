using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Stores.UpdateStore;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task UpdateStore_Should_Return_200_When_Request_Is_Valid()
        {
            AddAuthenticatedHeaders();

            var store = CreateStore();
            await SeedStoreAsync(store);

            var request = CreateValidRequest();

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{store.Id}",
                request,
                _jsonOptions,
                CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<UpdateStoreResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);

            Assert.AreEqual(store.Id, responseBody.Id);
            Assert.AreEqual(store.StoreNumber, responseBody.StoreNumber);
            Assert.AreEqual(request.Name, responseBody.Name);
            Assert.IsNotNull(responseBody.Address);
            Assert.AreEqual(request.Address.CountryCode, responseBody.Address.CountryCode);
            Assert.AreEqual(request.Address.PostalCode, responseBody.Address.PostalCode);
            Assert.AreEqual(request.Address.City, responseBody.Address.City);
            Assert.AreEqual(request.Address.Street, responseBody.Address.Street);
            Assert.AreEqual(request.Address.PublicSpace, responseBody.Address.PublicSpace);
            Assert.AreEqual(request.Address.HouseNumber, responseBody.Address.HouseNumber);

            await AssertStoreWasUpdatedAsync(store.Id, request);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_404_When_Store_Does_Not_Exist()
        {
            AddAuthenticatedHeaders();

            var missingStoreId = Guid.NewGuid();
            var request = CreateValidRequest();

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{missingStoreId}",
                request,
                _jsonOptions,
                CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(problemDetails);
            Assert.AreEqual($"{Endpoint}/{missingStoreId}", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_400_When_Store_Id_Is_Empty()
        {
            AddAuthenticatedHeaders();

            var request = CreateValidRequest();

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{Guid.Empty}",
                request,
                _jsonOptions,
                CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(problemDetails);
            Assert.AreEqual($"{Endpoint}/{Guid.Empty}", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_400_When_Request_Body_Is_Null()
        {
            AddAuthenticatedHeaders();

            var store = CreateStore();
            await SeedStoreAsync(store);

            using var content = new StringContent("null", Encoding.UTF8, "application/json");

            using var response = await _httpClient!.PutAsync(
                $"{Endpoint}/{store.Id}",
                content,
                CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            var request = CreateValidRequest();

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{Guid.NewGuid()}",
                request,
                _jsonOptions,
                CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_400_When_Name_Is_Empty()
        {
            AddAuthenticatedHeaders();

            var store = CreateStore();
            await SeedStoreAsync(store);

            var request = CreateValidRequest(name: string.Empty);

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{store.Id}",
                request,
                _jsonOptions,
                CancellationToken.None);

            await AssertBadRequestAsync(response, store.Id);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_400_When_Address_Is_Missing()
        {
            AddAuthenticatedHeaders();

            var store = CreateStore();
            await SeedStoreAsync(store);

            var request = CreateValidRequest(address: null!);

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{store.Id}",
                request,
                _jsonOptions,
                CancellationToken.None);

            await AssertBadRequestAsync(response, store.Id);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_400_When_Postal_Code_Is_Empty()
        {
            AddAuthenticatedHeaders();

            var store = CreateStore();
            await SeedStoreAsync(store);

            var request = CreateValidRequest(
                address: CreateValidAddress(postalCode: string.Empty));

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{store.Id}",
                request,
                _jsonOptions,
                CancellationToken.None);

            await AssertBadRequestAsync(response, store.Id);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_400_When_City_Is_Empty()
        {
            AddAuthenticatedHeaders();

            var store = CreateStore();
            await SeedStoreAsync(store);

            var request = CreateValidRequest(
                address: CreateValidAddress(city: string.Empty));

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{store.Id}",
                request,
                _jsonOptions,
                CancellationToken.None);

            await AssertBadRequestAsync(response, store.Id);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_400_When_Street_Is_Empty()
        {
            AddAuthenticatedHeaders();

            var store = CreateStore();
            await SeedStoreAsync(store);

            var request = CreateValidRequest(
                address: CreateValidAddress(street: string.Empty));

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{store.Id}",
                request,
                _jsonOptions,
                CancellationToken.None);

            await AssertBadRequestAsync(response, store.Id);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_400_When_Public_Space_Is_Unknown()
        {
            AddAuthenticatedHeaders();

            var store = CreateStore();
            await SeedStoreAsync(store);

            var invalidPublicSpace = (PublicSpaces)999;

            var request = CreateValidRequest(
                address: CreateValidAddress(
                    publicSpace: invalidPublicSpace));

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{store.Id}",
                request,
                _jsonOptions,
                CancellationToken.None);

            await AssertBadRequestAsync(response, store.Id);
        }

        [TestMethod]
        public async Task UpdateStore_Should_Return_400_When_House_Number_Is_Empty()
        {
            AddAuthenticatedHeaders();

            var store = CreateStore();
            await SeedStoreAsync(store);

            var request = CreateValidRequest(
                address: CreateValidAddress(
                    houseNumber: string.Empty));

            using var response = await _httpClient!.PutAsJsonAsync(
                $"{Endpoint}/{store.Id}",
                request,
                _jsonOptions,
                CancellationToken.None);

            await AssertBadRequestAsync(response, store.Id);
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

        private static UpdateStoreRequest CreateValidRequest(string name = "Pécs", AddressRequest? address = null)
        {
            return new UpdateStoreRequest(Name: name, Address: address ?? CreateValidAddress());
        }

        private static AddressRequest CreateValidAddress(
            CountryCodes countryCode = CountryCodes.HUN,
            string postalCode = "7634",
            string city = "Pécs",
            string street = "Makay István",
            PublicSpaces publicSpace = PublicSpaces.Street,
            string houseNumber = "11")
        {
            return new AddressRequest(
                CountryCode: countryCode,
                PostalCode: postalCode,
                City: city,
                Street: street,
                PublicSpace: publicSpace,
                HouseNumber: houseNumber);
        }

        private async Task AssertBadRequestAsync(HttpResponseMessage response, Guid storeId)
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("The request is invalid.", problemDetails.Title);
            Assert.AreEqual($"{Endpoint}/{storeId}", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }

        private void AddAuthenticatedHeaders()
        {
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeader, "true");
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraObjectIdHeader, Guid.NewGuid().ToString());
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraTenantIdHeader, Guid.NewGuid().ToString());
        }

        private async Task SeedStoreAsync(Store store)
        {
            using var scope = _applicationFactory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Stores.AddAsync(store, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        private async Task AssertStoreWasUpdatedAsync(Guid storeId, UpdateStoreRequest request)
        {
            using var scope = _applicationFactory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persistedStore = await dbContext.Stores
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    store => store.Id == storeId,
                    CancellationToken.None);

            Assert.IsNotNull(persistedStore);
            Assert.AreEqual(request.Name, persistedStore.Name);
            Assert.AreEqual(request.Address.CountryCode, persistedStore.Address.CountryCode);
            Assert.AreEqual(request.Address.PostalCode, persistedStore.Address.PostalCode);
            Assert.AreEqual(request.Address.City, persistedStore.Address.City);
            Assert.AreEqual(request.Address.Street, persistedStore.Address.Street);
            Assert.AreEqual(request.Address.PublicSpace, persistedStore.Address.PublicSpace);
            Assert.AreEqual(request.Address.HouseNumber, persistedStore.Address.HouseNumber);
        }
    }
}