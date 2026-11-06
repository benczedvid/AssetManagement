
using AssetManagement.Application.Assets.CreateAsset;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Domain.Entities.Vendors;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Tests.Builders;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetManagement.WebApi.IntegrationTests.Controllers.AssetControllers
{
    [TestClass]
    public sealed class CreateAssetEndpointTests
    {
        private AssetManagementWebApplicationFactory _applicationFactory = null!;
        private HttpClient _httpClient = null!;
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
            _applicationFactory?.Dispose();
            _httpClient?.Dispose();
        }
        [TestMethod]
        public async Task CreateAsset_Should_Return_201_Created()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            var request = new CreateAssetRequest(
                AssetName: "HUHQLAP0287",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: "21JN0008HV",
                SerialNumber: "PF4KVG84",
                MacAddress: "74:5D:22:3A:76:C7",
                WifimacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedEmployeeNumber: "103549",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id,
                AssignedVendorId: Guid.NewGuid(),
                RfidTagId: "123456789"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/assets", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<CreateAssetResponse>(_jsonOptions, CancellationToken.None);

            Assert.AreEqual(request.AssetName, responseBody!.AssetName);
            Assert.AreEqual(request.AssetType, responseBody.AssetType);
            Assert.AreEqual(request.AssetStatus, responseBody.AssetStatus);
            Assert.AreEqual(request.Manufacturer, responseBody.Manufacturer);
            Assert.AreEqual(request.Model, responseBody.Model);
            Assert.AreEqual(request.SerialNumber, responseBody.SerialNumber);
            Assert.AreEqual(request.MacAddress, responseBody.MacAddress);
            Assert.AreEqual(request.WifimacAddress, responseBody.WifimacAddress);
            Assert.AreEqual(request.Imei, responseBody.Imei);
            Assert.AreEqual(request.OperatingSystem, responseBody.OperatingSystem);
            Assert.AreEqual(request.OperatingSystemVersion, responseBody.OperatingSystemVersion);
            Assert.AreEqual(store.Id, responseBody.AssignedStoreId);
            Assert.AreEqual(user.Id, responseBody.AssignedUserId);
            Assert.AreEqual(request.AssignedVendorId, responseBody.AssignedVendorId);
            Assert.AreEqual(request.RfidTagId, responseBody.RfidTagId);
        }

        [TestMethod]
        public async Task CreateAsset_Should_Return_401_When_Request_Is_Not_Authenticaed()
        {
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            var request = new CreateAssetRequest(
                AssetName: "HUHQLAP0287",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: "21JN0008HV",
                SerialNumber: "PF4KVG84",
                MacAddress: "74:5D:22:3A:76:C7",
                WifimacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedEmployeeNumber: "103549",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id,
                AssignedVendorId: Guid.NewGuid(),
                RfidTagId: "123456"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/assets", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateAsset_Should_Return_409_When_Serial_Number_Already_Exists()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);
            var firstAsset = new CreateAssetRequest(
                AssetName: "HUHQLAP0287",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: "21JN0008HV",
                SerialNumber: "PF4KVG84",
                MacAddress: "74:5D:22:3A:76:C7",
                WifimacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedEmployeeNumber: "103549",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id,
                AssignedVendorId: Guid.NewGuid(),
                RfidTagId: "123456"
                );
            using var firstResponse = await _httpClient!.PostAsJsonAsync("/api/assets", firstAsset, CancellationToken.None);

            var secondAsset = new CreateAssetRequest(
                AssetName: "HUHQLAP0287",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: "21JN0008HV",
                SerialNumber: "PF4KVG84",
                MacAddress: "74:5D:22:3A:76:C7",
                WifimacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedEmployeeNumber: "103549",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id,
                AssignedVendorId: Guid.NewGuid(),
                RfidTagId: "123456"
                );
            using var secondResponse = await _httpClient!.PostAsJsonAsync("/api/assets", secondAsset, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Created, firstResponse.StatusCode);
            Assert.AreEqual(HttpStatusCode.Conflict, secondResponse.StatusCode);
        }

        [TestMethod]
        public async Task CreateAsset_Should_Return_400_When_Serial_Number_Is_Empty()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            var request = new CreateAssetRequest(
                AssetName: "HUHQLAP0287",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: "21JN0008HV",
                SerialNumber: string.Empty,
                MacAddress: "74:5D:22:3A:76:C7",
                WifimacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedEmployeeNumber: "103549",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id,
                AssignedVendorId: Guid.NewGuid(),
                RfidTagId: "123456"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/assets", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [TestMethod]
        public async Task CreateAsset_Should_Return_400_When_Asset_Name_Is_Empty()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            var request = new CreateAssetRequest(
                AssetName: string.Empty,
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: "21JN0008HV",
                SerialNumber: "PF4KVG84",
                MacAddress: "74:5D:22:3A:76:C7",
                WifimacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedEmployeeNumber: "103549",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id,
                AssignedVendorId: Guid.NewGuid(),
                RfidTagId: "123456"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/assets", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [TestMethod]
        public async Task CreateAsset_Should_Return_400_When_Asset_Type_Is_Unknown()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            var request = new CreateAssetRequest(
                AssetName: "HUHQLAP0287",
                AssetType: AssetType.Unknown,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: "21JN0008HV",
                SerialNumber: "PF4KVG84",
                MacAddress: "74:5D:22:3A:76:C7",
                WifimacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedEmployeeNumber: "103549",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id,
                AssignedVendorId: Guid.NewGuid(),
                RfidTagId: "123456"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/assets", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [TestMethod]
        public async Task CreateAsset_Should_Return_400_When_Asset_Status_Is_Unknown()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            var request = new CreateAssetRequest(
                AssetName: "HUHQLAP0287",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Unknown,
                Manufacturer: "Lenovo",
                Model: "21JN0008HV",
                SerialNumber: "PF4KVG84",
                MacAddress: "74:5D:22:3A:76:C7",
                WifimacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedEmployeeNumber: "103549",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id,
                AssignedVendorId: Guid.NewGuid(),
                RfidTagId: "123456"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/assets", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [TestMethod]
        public async Task CreateAsset_Should_Return_400_When_Manufacturer_Is_Empty()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            var request = new CreateAssetRequest(
                AssetName: "HUHQLAP0287",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: string.Empty,
                Model: "21JN0008HV",
                SerialNumber: "PF4KVG84",
                MacAddress: "74:5D:22:3A:76:C7",
                WifimacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedEmployeeNumber: "103549",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id,
                AssignedVendorId: Guid.NewGuid(),
                RfidTagId: "123456"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/assets", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [TestMethod]
        public async Task CreateAsset_Should_Return_400_When_Model_Is_Empty()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            var request = new CreateAssetRequest(
                AssetName: "HUHQLAP0287",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: string.Empty,
                SerialNumber: "PF4KVG84",
                MacAddress: "74:5D:22:3A:76:C7",
                WifimacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedEmployeeNumber: "103549",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id,
                AssignedVendorId: Guid.NewGuid(),
                RfidTagId: "123456"
                );

            using var response = await _httpClient!.PostAsJsonAsync("/api/assets", request, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }


        private void AddAuthenticatedHeaders()
        {
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeader, "true");
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraObjectIdHeader, Guid.NewGuid().ToString());
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraTenantIdHeader, Guid.NewGuid().ToString());
        }

        private async Task SeedStore(Store store)
        {
            using var scope = _applicationFactory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Stores.AddAsync(store, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        private async Task SeedUser(ApplicationUser user)
        {
            using var scope = _applicationFactory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.ApplicationUsers.AddAsync(user, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }
    }
}
