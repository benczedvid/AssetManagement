
using AssetManagement.Application.Assets.UpdateAsset;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Tests.Builders;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetManagement.WebApi.IntegrationTests.Controllers.AssetControllers
{
    [TestClass]
    public sealed class UpdateAssetEndpointTests
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
        public async Task UpdateAsset_Should_Return_200_When_Request_Is_Valid()
        {
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            AddAuthenticatedHeaders();
            var asset = Asset.Create(
                assetName: "HUHQLAP0287",
                assetType: AssetType.Notebook,
                assetStatus: AssetStatus.Used_In_HQ,
                manufacturer: "Lenovo",
                model: "21JN0008HV",
                serialNumber: "PF4KVG84",
                macAddress: "74:5D:22:3A:76:C7",
                wifiMacAddress: "00:00:00:00:00:00",
                imei: "123456789123456",
                operatingSystem: "Windows",
                operatingSystemVersion: "24H2",
                assignedStoreId: store.Id,
                assignedUserId: user.Id,
                );
            await SeedAssetAsync(asset);

            var updateAsset = new UpdateAssetRequest(
                AssetName: "HUHQLAP0288",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: "21JN0009HV",
                MacAddress: "74:5D:22:3A:76:C7",
                WifiMacAddress: "00:00:00:00:00:00",
                Imei: "123456789123457",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id);
            var response = await _httpClient!.PutAsJsonAsync($"/api/assets/{asset.Id}", updateAsset, CancellationToken.None);
            var responseBody = await response.Content.ReadFromJsonAsync<UpdateAssetResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(updateAsset.AssetName, responseBody!.AssetName);
            Assert.AreEqual(updateAsset.AssetType, responseBody.AssetType);
            Assert.AreEqual(updateAsset.AssetStatus, responseBody.AssetStatus);
            Assert.AreEqual(updateAsset.Manufacturer, responseBody.Manufacturer);
            Assert.AreEqual(updateAsset.Model, responseBody.Model);
            Assert.AreEqual(asset.SerialNumber, responseBody.SerialNumber);
            Assert.AreEqual(updateAsset.MacAddress, responseBody.MacAddress);
            Assert.AreEqual(updateAsset.WifiMacAddress, responseBody.WifiMacAddress);
            Assert.AreEqual(updateAsset.Imei, responseBody.Imei);
            Assert.AreEqual(updateAsset.OperatingSystem, responseBody.OperatingSystem);
            Assert.AreEqual(updateAsset.OperatingSystemVersion, responseBody.OperatingSystemVersion);
            Assert.AreEqual(store.Id, responseBody.AssignedStoreId);
            Assert.AreEqual(user.Id, responseBody.AssignedUserId);
        }

        [TestMethod]
        public async Task UpdateAsset_Should_Return_404_When_Asset_Does_Not_Exist()
        {
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            AddAuthenticatedHeaders();

            Guid invalidId = Guid.NewGuid();

            var updateAsset = new UpdateAssetRequest(
                AssetName: "HUHQLAP0288",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: "21JN0009HV",
                MacAddress: "74:5D:22:3A:76:C7",
                WifiMacAddress: "00:00:00:00:00:00",
                Imei: "123456789123457",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id);

            var response = await _httpClient!.PutAsJsonAsync($"/api/assets/{invalidId}", updateAsset, CancellationToken.None);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task UpdateAsset_Should_Return_400_When_Request_Body_Is_Null()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);


            var asset = Asset.Create(
                assetName: "HUHQLAP0287",
                assetType: AssetType.Notebook,
                assetStatus: AssetStatus.Used_In_HQ,
                manufacturer: "Lenovo",
                model: "21JN0008HV",
                serialNumber: "PF4KVG84",
                macAddress: "74:5D:22:3A:76:C7",
                wifiMacAddress: "00:00:00:00:00:00",
                imei: "123456789123456",
                operatingSystem: "Windows",
                operatingSystemVersion: "24H2",
                assignedStoreId: store.Id,
                assignedUserId: user.Id
                );
            await SeedAssetAsync(asset);
            using var content = new StringContent("null", Encoding.UTF8, "application/json");

            var response = await _httpClient!.PutAsync($"/api/assets/{asset.Id}", content, CancellationToken.None);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [TestMethod]
        public async Task UpdateAsset_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            await SeedStore(store);
            await SeedUser(user);

            var asset = Asset.Create(
                assetName: "HUHQLAP0287",
                assetType: AssetType.Notebook,
                assetStatus: AssetStatus.Used_In_HQ,
                manufacturer: "Lenovo",
                model: "21JN0008HV",
                serialNumber: "PF4KVG84",
                macAddress: "74:5D:22:3A:76:C7",
                wifiMacAddress: "00:00:00:00:00:00",
                imei: "123456789123456",
                operatingSystem: "Windows",
                operatingSystemVersion: "24H2",
                assignedStoreId: store.Id,
                assignedUserId: user.Id
                );
            await SeedAssetAsync(asset);

            var updateAsset = new UpdateAssetRequest(
                AssetName: "HUHQLAP0288",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.Used_In_HQ,
                Manufacturer: "Lenovo",
                Model: "21JN0009HV",
                MacAddress: "74:5D:22:3A:76:C7",
                WifiMacAddress: "00:00:00:00:00:00",
                Imei: "123456789123457",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedStoreId: store.Id,
                AssignedUserId: user.Id);

            using var response = await _httpClient!.PutAsJsonAsync($"/api/assets/{asset.Id}", updateAsset, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        private void AddAuthenticatedHeaders()
        {
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeader, "true");
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraObjectIdHeader, Guid.NewGuid().ToString());
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraTenantIdHeader, Guid.NewGuid().ToString());
        }

        private async Task SeedAssetAsync(Asset asset)
        {
            using var scope = _applicationFactory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Assets.AddAsync(asset, CancellationToken.None);

            await dbContext.SaveChangesAsync(CancellationToken.None);
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