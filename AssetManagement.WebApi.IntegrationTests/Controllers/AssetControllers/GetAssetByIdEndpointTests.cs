
using AssetManagement.Application.Assets.GetAsset;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Tests.Builders;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetManagement.WebApi.IntegrationTests.Controllers.AssetControllers
{
    [TestClass]
    public sealed class GetAssetByIdEndpointTests
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
        public async Task GetAssetById_Should_Return_200_When_Asset_Exist()
        {
            AddAuthenticatedHeaders();
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();
            await SeedStore(store);
            await SeedUser(user);
            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedUserId(user.Id)
                .Build();
            await SeedAsset(asset);

            using var response = await _httpClient!.GetAsync($"/api/assets/{asset.Id}", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<GetAssetResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.AreEqual(asset.Id, responseBody.AssetId);
            Assert.AreEqual(asset.AssetName, responseBody.AssetName);
        }

        [TestMethod]
        public async Task GetAssetById_Should_Return_404_When_Asset_Does_Not_Exist()
        {
            AddAuthenticatedHeaders();
            Guid unknownId = Guid.NewGuid();

            using var response = await _httpClient!.GetAsync($"/api/assets/{unknownId}", CancellationToken.None);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken.None);
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("The requested resource was not found.", problemDetails.Title);
            Assert.AreEqual($"/api/assets/{unknownId}", problemDetails.Instance); 
            Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
        }

        [TestMethod]
        public async Task GetAssetById_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();
            await SeedStore(store);
            await SeedUser(user);
            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedUserId(user.Id)
                .Build();
            await SeedAsset(asset);
            using var response = await _httpClient!.GetAsync($"/api/assets/{asset.Id}", CancellationToken.None);

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