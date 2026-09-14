using AssetManagement.Application.Assets.GetAssets;
using AssetManagement.Application.Stores.GetStores;
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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetManagement.WebApi.IntegrationTests.Controllers.AssetControllers
{
    [TestClass]
    public sealed class GetAssetsEndpointTests
    {
        private DeathStarWebApplicationFactory _applicationFactory = null!;
        private HttpClient _httpClient = null!;
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
        public async Task GetAssets_Should_Return_200_With_All_Stores()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();
            await SeedStore(store);
            await SeedUser(user);

            var firstAsset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedUserId(user.Id)
                .Build();
            await SeedAsset(firstAsset);
            var secondAssest = new AssetBuilder()
                .WithSerialNumber("12345")
                .WithAssignedStoreId(store.Id)
                .WithAssignedUserId(user.Id)
                .Build();
            await SeedAsset(secondAssest);

            using var response = await _httpClient!.GetAsync("/api/assets", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<IReadOnlyList<GetAssetsResponse>>(_jsonOptions, CancellationToken.None);
            Assert.IsNotNull(responseBody);
            Assert.HasCount(2, responseBody);

            var firstAssetInResponse = responseBody.Single(asset => asset.AssetId == firstAsset.Id);
            Assert.AreEqual(firstAsset.AssetName, firstAssetInResponse.AssetName);
            Assert.AreEqual(firstAsset.AssetType, firstAssetInResponse.AssetType);
            Assert.AreEqual(firstAsset.AssetStatus, firstAssetInResponse.AssetStatus);
            Assert.AreEqual(firstAsset.Manufacturer, firstAssetInResponse.Manufacturer);
            Assert.AreEqual(firstAsset.Model, firstAssetInResponse.Model);
            Assert.AreEqual(firstAsset.MacAddress, firstAssetInResponse.MacAddress);
            Assert.AreEqual(firstAsset.WiFiMacAddress, firstAssetInResponse.WifiMacAddress);
            Assert.AreEqual(firstAsset.Imei, firstAssetInResponse.Imei);
            Assert.AreEqual(firstAsset.OperatingSystem, firstAssetInResponse.OperatingSystem);
            Assert.AreEqual(firstAsset.OperatingSystemVersion, firstAssetInResponse.OperatingSystemVersion);
            Assert.AreEqual(firstAsset.AssignedStoreId, firstAssetInResponse.AssignedStoreId);
            Assert.AreEqual(firstAsset.AssignedUserId, firstAssetInResponse.AssignedUserId);
        }

        [TestMethod]
        public async Task GetAssets_Should_Return_200_With_Empty_List_When_No_Stores_Exist()
        {
            AddAuthenticatedHeaders();
            using var response = await _httpClient!.GetAsync("/api/assets", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<IReadOnlyList<GetStoresResponse>>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.HasCount(0, responseBody);
        }

        [TestMethod]
        public async Task GetAssets_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            using var response = await _httpClient!.GetAsync("/api/assets", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
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
        private async Task SeedAsset(Asset asset)
        {
            using var scope = _applicationFactory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Assets.AddAsync(asset, CancellationToken.None);

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }
}