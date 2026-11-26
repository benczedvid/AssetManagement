using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Dashboard.GetStoreDashboard;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Dashboard.GetStoreDashboard
{
    [TestClass]
    public sealed class GetStoreDashboardHandlerTests
    {
        private FakeStoreRepository _storeRepository = null!;
        private FakeAssetRepository _assetRepository = null!;
        private FakeUserAccessScopeResolver _userAccessScopeResolver = null!;
        private GetStoreDashboardHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _storeRepository = new FakeStoreRepository();
            _assetRepository = new FakeAssetRepository();
            _userAccessScopeResolver = new FakeUserAccessScopeResolver();

            _handler = new GetStoreDashboardHandler(
                _storeRepository,
                _assetRepository,
                _userAccessScopeResolver);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_StoreRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetStoreDashboardHandler(null!, _assetRepository, _userAccessScopeResolver));

            Assert.AreEqual("storeRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetStoreDashboardHandler(_storeRepository, null!, _userAccessScopeResolver));

            Assert.AreEqual("assetRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_UserAccessScopeResolver_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetStoreDashboardHandler(_storeRepository, _assetRepository, null!));

            Assert.AreEqual("userAccessScopeResolver", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_All_Stores_Ordered_By_StoreNumber_For_Global_Scope()
        {
            SeedStore("322", "Budaörs");
            SeedStore("101", "Debrecen");
            SeedStore("210", "Győr");

            var response = await _handler.HandleAsync(CancellationToken.None);

            CollectionAssert.AreEqual(
                new[] { "101", "210", "322" },
                response.Select(store => store.StoreNumber).ToArray());

            Assert.AreEqual(1, _storeRepository.GetAllCallCount);
            Assert.AreEqual(1, _assetRepository.GetAllCallCount);
            Assert.AreEqual(0, _assetRepository.GetAllByStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Count_Pdts_By_Status()
        {
            var store = SeedStore("321", "Budapest M3");

            SeedPdt(store.Id, AssetStatus.In_Store);
            SeedPdt(store.Id, AssetStatus.In_Store);
            SeedPdt(store.Id, AssetStatus.Used_In_Store);
            SeedPdt(store.Id, AssetStatus.In_Service);
            SeedPdt(store.Id, AssetStatus.Route_To_Store);

            var response = await _handler.HandleAsync(CancellationToken.None);

            var dashboard = response.Single();

            Assert.AreEqual(store.Id, dashboard.StoreId);
            Assert.AreEqual("321", dashboard.StoreNumber);
            Assert.AreEqual("Budapest M3", dashboard.StoreName);
            Assert.AreEqual(5, dashboard.TotalPDTs);
            Assert.AreEqual(2, dashboard.AvailablePDTs);
            Assert.AreEqual(1, dashboard.UsedPDTs);
            Assert.AreEqual(1, dashboard.ServicePDTs);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Exclude_Disposed_Pdts()
        {
            var store = SeedStore("321", "Budapest M3");

            SeedPdt(store.Id, AssetStatus.In_Store);
            SeedPdt(store.Id, AssetStatus.Disposed);

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.AreEqual(1, response.Single().TotalPDTs);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Exclude_Non_Pdt_Assets()
        {
            var store = SeedStore("321", "Budapest M3");

            SeedPdt(store.Id, AssetStatus.In_Store);

            _assetRepository.Seed(
                new AssetBuilder()
                    .WithAssetType(AssetType.Notebook)
                    .WithAssetStatus(AssetStatus.In_Store)
                    .WithAssignedStoreId(store.Id)
                    .WithSerialNumber(Guid.NewGuid().ToString("N"))
                    .Build());

            var response = await _handler.HandleAsync(CancellationToken.None);

            var dashboard = response.Single();

            Assert.AreEqual(1, dashboard.TotalPDTs);
            Assert.AreEqual(1, dashboard.AvailablePDTs);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Zero_Counts_For_Store_Without_Pdts()
        {
            var storeWithPdts = SeedStore("321", "Budapest M3");
            var storeWithoutPdts = SeedStore("322", "Budaörs");

            SeedPdt(storeWithPdts.Id, AssetStatus.In_Store);

            var response = await _handler.HandleAsync(CancellationToken.None);

            var emptyDashboard = response.Single(store => store.StoreId == storeWithoutPdts.Id);

            Assert.AreEqual(0, emptyDashboard.TotalPDTs);
            Assert.AreEqual(0, emptyDashboard.AvailablePDTs);
            Assert.AreEqual(0, emptyDashboard.UsedPDTs);
            Assert.AreEqual(0, emptyDashboard.ServicePDTs);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Only_Users_Store_For_Store_Scope()
        {
            var ownStore = SeedStore("321", "Budapest M3");
            var otherStore = SeedStore("322", "Budaörs");

            SeedPdt(ownStore.Id, AssetStatus.In_Store);
            SeedPdt(otherStore.Id, AssetStatus.In_Store);

            _userAccessScopeResolver.SetStoreScope(ownStore.Id);

            var response = await _handler.HandleAsync(CancellationToken.None);

            var dashboard = response.Single();

            Assert.AreEqual(ownStore.Id, dashboard.StoreId);
            Assert.AreEqual(1, dashboard.TotalPDTs);
            Assert.AreEqual(0, _storeRepository.GetAllCallCount);
            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetAllCallCount);
            Assert.AreEqual(1, _assetRepository.GetAllByStoreIdCallCount);
            Assert.AreEqual(ownStore.Id, _assetRepository.LastRequestedStoreId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Users_Store_Does_Not_Exist()
        {
            _userAccessScopeResolver.SetStoreScope(Guid.NewGuid());

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _handler.HandleAsync(CancellationToken.None));

            Assert.AreEqual(0, _assetRepository.GetAllByStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Scope_Has_No_Store()
        {
            _userAccessScopeResolver.SetScope(new UserAccessScope(CanAccessAllStores: false, StoreId: null));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _handler.HandleAsync(CancellationToken.None));

            Assert.AreEqual(0, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetAllByStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Empty_List_When_No_Stores_Exist()
        {
            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.IsEmpty(response);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Dependencies()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(cancellationToken);

            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _storeRepository.LastGetAllCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastGetAllCancellationToken);
        }

        private Store SeedStore(string storeNumber, string name)
        {
            var store = new StoreBuilder()
                .WithStoreNumber(storeNumber)
                .WithName(name)
                .Build();

            _storeRepository.Seed(store);

            return store;
        }

        private void SeedPdt(Guid storeId, AssetStatus status)
        {
            _assetRepository.Seed(
                new AssetBuilder()
                    .WithAssetType(AssetType.PDT)
                    .WithAssetStatus(status)
                    .WithAssignedStoreId(storeId)
                    .WithSerialNumber(Guid.NewGuid().ToString("N"))
                    .WithRfidTagId(Guid.NewGuid().ToString("N"))
                    .Build());
        }
    }
}
