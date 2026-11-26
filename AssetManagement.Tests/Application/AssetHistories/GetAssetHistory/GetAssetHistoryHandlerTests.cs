using AssetManagement.Application.AssetHistories.GetAssetHistory;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.AssetHistories.GetAssetHistory
{
    [TestClass]
    public sealed class GetAssetHistoryHandlerTests
    {
        private FakeAssetHistoryRepository _assetHistoryRepository = null!;
        private FakeAssetRepository _assetRepository = null!;
        private FakeUserAccessScopeResolver _userAccessScopeResolver = null!;
        private GetAssetHistoryHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _assetHistoryRepository = new FakeAssetHistoryRepository();
            _assetRepository = new FakeAssetRepository();
            _userAccessScopeResolver = new FakeUserAccessScopeResolver();

            _handler = new GetAssetHistoryHandler(
                _assetHistoryRepository,
                _assetRepository,
                _userAccessScopeResolver);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetHistoryRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetAssetHistoryHandler(null!, _assetRepository, _userAccessScopeResolver));

            Assert.AreEqual("assetHistoryRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetAssetHistoryHandler(_assetHistoryRepository, null!, _userAccessScopeResolver));

            Assert.AreEqual("assetRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_UserAccessScopeResolver_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetAssetHistoryHandler(_assetHistoryRepository, _assetRepository, null!));

            Assert.AreEqual("userAccessScopeResolver", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_AssetId_Is_Empty()
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => _handler.HandleAsync(Guid.Empty, CancellationToken.None));

            Assert.AreEqual("assetId", exception.ParamName);
            Assert.AreEqual(0, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(0, _assetHistoryRepository.GetByAssetIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_History_For_Global_Scope()
        {
            var asset = new AssetBuilder().Build();

            _assetRepository.Seed(asset);

            await _assetHistoryRepository.AddRangeAsync(
            [
                new AssetHistoryBuilder().WithAssetId(asset.Id).Build(),
                new AssetHistoryBuilder().WithAssetId(asset.Id).WithPropertyName("Model").Build(),
                new AssetHistoryBuilder().WithAssetId(Guid.NewGuid()).Build()
            ]);

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.HasCount(2, response);
            Assert.IsTrue(response.All(history => history.AssetId == asset.Id));
            Assert.AreEqual(1, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Map_History_Fields()
        {
            var asset = new AssetBuilder().Build();
            var changedByUserId = Guid.NewGuid();
            var changedAtUtc = new DateTime(2026, 11, 20, 9, 30, 0, DateTimeKind.Utc);

            _assetRepository.Seed(asset);

            var history = new AssetHistoryBuilder()
                .WithAssetId(asset.Id)
                .WithAction("Updated")
                .WithPropertyName("SerialNumber")
                .WithOldValue("OLD-001")
                .WithNewValue("NEW-001")
                .WithChangedByUserId(changedByUserId)
                .WithChangedByUserName("Test User")
                .WithChangedAtUtc(changedAtUtc)
                .Build();

            await _assetHistoryRepository.AddRangeAsync([history]);

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            var result = response.Single();

            Assert.AreEqual(history.Id, result.Id);
            Assert.AreEqual(asset.Id, result.AssetId);
            Assert.AreEqual("Updated", result.Action);
            Assert.AreEqual("SerialNumber", result.PropertyName);
            Assert.AreEqual("OLD-001", result.OldValue);
            Assert.AreEqual("NEW-001", result.NewValue);
            Assert.AreEqual(changedByUserId, result.ChangedByUserId);
            Assert.AreEqual("Test User", result.ChangedByUserName);
            Assert.AreEqual(changedAtUtc, result.ChangedAtUtc);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Preserve_Repository_Order()
        {
            var asset = new AssetBuilder().Build();

            _assetRepository.Seed(asset);

            var older = new AssetHistoryBuilder()
                .WithAssetId(asset.Id)
                .WithChangedAtUtc(new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc))
                .Build();

            var newer = new AssetHistoryBuilder()
                .WithAssetId(asset.Id)
                .WithChangedAtUtc(new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc))
                .Build();

            await _assetHistoryRepository.AddRangeAsync([older, newer]);

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.AreEqual(newer.Id, response[0].Id);
            Assert.AreEqual(older.Id, response[1].Id);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Empty_List_When_Asset_Has_No_History()
        {
            var asset = new AssetBuilder().Build();

            _assetRepository.Seed(asset);

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.IsEmpty(response);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Asset_Does_Not_Exist()
        {
            var assetId = Guid.NewGuid();

            var exception = await Assert.ThrowsExactlyAsync<AssetNotFoundException>(
                () => _handler.HandleAsync(assetId, CancellationToken.None));

            Assert.AreEqual(assetId, exception.Id);
            Assert.AreEqual(0, _assetHistoryRepository.GetByAssetIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_History_When_Asset_Belongs_To_Users_Store()
        {
            var storeId = Guid.NewGuid();
            var asset = new AssetBuilder().WithAssignedStoreId(storeId).Build();

            _assetRepository.Seed(asset);
            _userAccessScopeResolver.SetStoreScope(storeId);

            await _assetHistoryRepository.AddRangeAsync([new AssetHistoryBuilder().WithAssetId(asset.Id).Build()]);

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.HasCount(1, response);
            Assert.AreEqual(0, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(1, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(storeId, _assetRepository.LastRequestedStoreId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Asset_Belongs_To_Another_Store()
        {
            var asset = new AssetBuilder().WithAssignedStoreId(Guid.NewGuid()).Build();

            _assetRepository.Seed(asset);
            _userAccessScopeResolver.SetStoreScope(Guid.NewGuid());

            await _assetHistoryRepository.AddRangeAsync([new AssetHistoryBuilder().WithAssetId(asset.Id).Build()]);

            var exception = await Assert.ThrowsExactlyAsync<AssetNotFoundException>(
                () => _handler.HandleAsync(asset.Id, CancellationToken.None));

            Assert.AreEqual(asset.Id, exception.Id);
            Assert.AreEqual(0, _assetHistoryRepository.GetByAssetIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Scope_Has_No_Store()
        {
            var asset = new AssetBuilder().Build();

            _assetRepository.Seed(asset);
            _userAccessScopeResolver.SetScope(new UserAccessScope(CanAccessAllStores: false, StoreId: null));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _handler.HandleAsync(asset.Id, CancellationToken.None));

            Assert.AreEqual(0, _assetHistoryRepository.GetByAssetIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Dependencies()
        {
            var asset = new AssetBuilder().Build();

            _assetRepository.Seed(asset);

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(asset.Id, cancellationToken);

            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastGetByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _assetHistoryRepository.LastGetByAssetIdCancellationToken);
        }
    }
}
