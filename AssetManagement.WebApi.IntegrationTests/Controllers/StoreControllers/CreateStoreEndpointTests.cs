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
        public async Task CreateStore_Should_Return_201_Created()
        {
            AddAuthenticatedHeaders();

            var request = new CreateStoreRequest(
                StoreNumber: "321",
                Name: "Budapest M3",
                CountryCode: CountryCodes.HUN,
                PostalCode: "1152",
                City: "Budapest",
                Address: "Városkapu",
                PublicSpace: PublicSpaces.utca,
                HouseNumber: "5"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/stores", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<CreateStoreResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);

            Assert.AreNotEqual(Guid.Empty, responseBody.Id);
            Assert.AreEqual(request.StoreNumber, responseBody.StoreNumber);
            Assert.AreEqual(request.Name, responseBody.Name);
            Assert.AreEqual(request.CountryCode, responseBody.CountryCode);
            Assert.AreEqual(request.PostalCode, responseBody.PostalCode);
            Assert.AreEqual(request.City, responseBody.City);
            Assert.AreEqual(request.Address, responseBody.Address);
            Assert.AreEqual(request.PublicSpace, responseBody.PublicSpace);
            Assert.AreEqual(request.HouseNumber, responseBody.HouseNumber);

            await AssertStoreWasPersistedAsync(responseBody);
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            var request = new CreateStoreRequest(
                StoreNumber: "321",
                Name: "Budapest M3",
                CountryCode: CountryCodes.HUN,
                PostalCode: "1152",
                City: "Budapest",
                Address: "Városkapu",
                PublicSpace: PublicSpaces.utca,
                HouseNumber: "5"
            );
            using var response = await _httpClient!.PostAsJsonAsync("/api/stores", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_409_When_Store_Number_Already_Exists()
        {
            AddAuthenticatedHeaders();
            var firstRequest = new CreateStoreRequest(
                StoreNumber: "321",
                Name: "Budapest M3",
                CountryCode: CountryCodes.HUN,
                PostalCode: "1152",
                City: "Budapest",
                Address: "Városkapu",
                PublicSpace: PublicSpaces.utca,
                HouseNumber: "5"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/stores", firstRequest, CancellationToken.None);

            var secondRequest = new CreateStoreRequest(
                StoreNumber: "321",
                Name: "Budapest M3",
                CountryCode: CountryCodes.HUN,
                PostalCode: "1152",
                City: "Budapest",
                Address: "Városkapu",
                PublicSpace: PublicSpaces.utca,
                HouseNumber: "5"
                );
            using var secondResponse = await _httpClient!.PostAsJsonAsync("/api/stores", secondRequest, CancellationToken.None);

            var problemDetails = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            Assert.AreEqual(HttpStatusCode.Conflict, secondResponse.StatusCode);

            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("A resource conflict occurred.", problemDetails.Title);
            Assert.AreEqual("/api/stores", problemDetails.Instance);

        }

        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_Store_Number_Is_Invalid()
        {
            AddAuthenticatedHeaders();
            var request = new CreateStoreRequest(
                StoreNumber: string.Empty,
                Name: "Budapest M3",
                CountryCode: CountryCodes.HUN,
                PostalCode: "1152",
                City: "Budapest",
                Address: "Városkapu",
                PublicSpace: PublicSpaces.utca,
                HouseNumber: "5"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/stores", request, CancellationToken.None);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("The request is invalid.", problemDetails.Title);
            Assert.AreEqual("/api/stores", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }
        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_Name_Is_Missing()
        {
            AddAuthenticatedHeaders();
            var request = new CreateStoreRequest(
                StoreNumber: "321",
                Name: string.Empty,
                CountryCode: CountryCodes.HUN,
                PostalCode: "1152",
                City: "Budapest",
                Address: "Városkapu",
                PublicSpace: PublicSpaces.utca,
                HouseNumber: "5"
                );
            using var response = await _httpClient!.PostAsJsonAsync("/api/stores", request, CancellationToken.None);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("The request is invalid.", problemDetails.Title);
            Assert.AreEqual("/api/stores", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }

        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_Store_City_Is_Empty(){
            AddAuthenticatedHeaders();
            var request = new CreateStoreRequest(
                StoreNumber: "321",
                Name: "Budapest M3",
                CountryCode: CountryCodes.HUN,
                PostalCode: "1152",
                City: string.Empty,
                Address: "Városkapu",
                PublicSpace: PublicSpaces.utca,
                HouseNumber: "5"
                );
            using var response = await _httpClient!.PostAsJsonAsync("/api/stores", request, CancellationToken.None);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("The request is invalid.", problemDetails.Title);
            Assert.AreEqual("/api/stores", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }
        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_Address_Is_Missing()
        {
            AddAuthenticatedHeaders();
            var request = new CreateStoreRequest(
                StoreNumber: "321",
                Name: "Budapest M3",
                CountryCode: CountryCodes.HUN,
                PostalCode: "1152",
                City: "Budapest",
                Address: string.Empty,
                PublicSpace: PublicSpaces.utca,
                HouseNumber: "5"
                );
            using var response = await _httpClient!.PostAsJsonAsync("/api/stores", request, CancellationToken.None);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("The request is invalid.", problemDetails.Title);
            Assert.AreEqual("/api/stores", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }
        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_TypeOfRoad_Is_unknown()
        {
            AddAuthenticatedHeaders();
            var request = new CreateStoreRequest(
                StoreNumber: "321",
                Name: "Budapest M3",
                CountryCode: CountryCodes.HUN,
                PostalCode: "1152",
                City: "Budapest",
                Address: "Városkapu",
                PublicSpace: PublicSpaces.unknown,
                HouseNumber: "5"
                );
            using var response = await _httpClient!.PostAsJsonAsync("/api/stores", request, CancellationToken.None);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("The request is invalid.", problemDetails.Title);
            Assert.AreEqual("/api/stores", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }
        [TestMethod]
        public async Task CreateStore_Should_Return_400_When_House_Number_Is_Invalid()
        {
            AddAuthenticatedHeaders();
            var request = new CreateStoreRequest(
                StoreNumber: "321",
                Name: "Budapest M3",
                CountryCode: CountryCodes.HUN,
                PostalCode: "1152",
                City: "Budapest",
                Address: "Városkapu",
                PublicSpace: PublicSpaces.utca,
                HouseNumber: string.Empty
                );
            using var response = await _httpClient!.PostAsJsonAsync("/api/stores", request, CancellationToken.None);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("The request is invalid.", problemDetails.Title);
            Assert.AreEqual("/api/stores", problemDetails.Instance);
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }



        private void AddAuthenticatedHeaders()
        {
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeader, "true");
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraObjectIdHeader, Guid.NewGuid().ToString());
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraTenantIdHeader, Guid.NewGuid().ToString());
        }
        private async Task AssertStoreWasPersistedAsync(CreateStoreResponse response)
        {
            using var scope = _applicationFactory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persistedStore = await dbContext.Stores.AsNoTracking().SingleOrDefaultAsync(store => store.Id == response.Id, CancellationToken.None);

            Assert.IsNotNull(persistedStore);
            Assert.AreEqual(response.StoreNumber, persistedStore.StoreNumber);
            Assert.AreEqual(response.Name, persistedStore.Name);
            Assert.AreEqual(response.City, persistedStore.City);
            Assert.AreEqual(response.Address, persistedStore.Address);
            Assert.AreEqual(response.PublicSpace, persistedStore.PublicSpace);
            Assert.AreEqual(response.HouseNumber, persistedStore.HouseNumber);
        }
    }
}
