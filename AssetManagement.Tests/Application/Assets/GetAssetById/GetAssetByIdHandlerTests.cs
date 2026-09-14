using AssetManagement.Application.Assets;
using AssetManagement.Application.Assets.GetAsset;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Assets.GetAssetById
{
    [TestClass]
    public sealed class GetAssetByIdHandlerTests
    {
        private FakeAssetRepository _assetRepository = null!;
        private FakeUserAccessScopeResolver _userAccessScopeResolver = null!;
        private GetAssetByIdHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _assetRepository = new FakeAssetRepository();
            _userAccessScopeResolver = new FakeUserAccessScopeResolver();
            _handler = new GetAssetByIdHandler(_assetRepository, _userAccessScopeResolver);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetAssetByIdHandler(null!, _userAccessScopeResolver));

            Assert.AreEqual("assetRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AccessScopeResolver_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetAssetByIdHandler(_assetRepository, null!));

            Assert.AreEqual("userAccessScopeResolver", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_AssetId_Is_Empty()
        {
            var exception = await Assert.ThrowsExactlyAsync<
                    ArgumentException>(() => _handler.HandleAsync(Guid.Empty, CancellationToken.None));

            Assert.AreEqual("id", exception.ParamName);
            Assert.AreEqual(0, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Asset_For_Global_Scope()
        {
            var asset = new AssetBuilder()
                .WithRfidTagId("RFID-0001")
                .WithAssignedVendorId(Guid.NewGuid())
                .Build();

            _assetRepository.Seed(asset);
            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(asset.Id, response.AssetId);
            Assert.AreEqual(asset.AssetName, response.AssetName);
            Assert.AreEqual(asset.AssetType, response.AssetType);
            Assert.AreEqual(asset.AssetStatus, response.AssetStatus);
            Assert.AreEqual(asset.Manufacturer, response.Manufacturer);
            Assert.AreEqual(asset.Model, response.Model);
            Assert.AreEqual(asset.SerialNumber, response.SerialNumber);
            Assert.AreEqual(asset.AssignedStoreId, response.AssignedStoreId);
            Assert.AreEqual(asset.AssignedUserId, response.AssignedUserId);
            Assert.AreEqual(asset.AssignedVendorId, response.AssignedVendorId);
            Assert.AreEqual(asset.RfidTagId, response.RfidTagId);
            Assert.AreEqual(asset.MacAddress, response.MacAddress);
            Assert.AreEqual(asset.WiFiMacAddress, response.WifiMacAddress);
            Assert.AreEqual(asset.Imei, response.Imei);
            Assert.AreEqual(asset.OperatingSystem, response.OperatingSystem);
            Assert.AreEqual(asset.OperatingSystemVersion, response.OperatingSystemVersion);
            Assert.AreEqual(asset.CreatedAtUtc, response.CreatedAtUtc);
            Assert.AreEqual(1, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(1, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(asset.Id,_assetRepository.LastRequestedAssetId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Asset_For_Matching_Store_Scope()
        {
            var storeId = Guid.NewGuid();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(storeId)
                .Build();

            _assetRepository.Seed(asset);

            _userAccessScopeResolver.SetStoreScope(
                storeId);

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.AreEqual(asset.Id, response.AssetId);
            Assert.AreEqual(storeId, response.AssignedStoreId);
            Assert.AreEqual(1, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(1, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(asset.Id, _assetRepository.LastRequestedAssetId);
            Assert.AreEqual(storeId, _assetRepository.LastRequestedStoreId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Asset_Does_Not_Exist_For_Global_Scope()
        {
            var unknownAssetId = Guid.NewGuid();

            _userAccessScopeResolver.SetGlobalScope();

            var exception = await Assert.ThrowsExactlyAsync< AssetNotFoundException>(() => _handler.HandleAsync(unknownAssetId, CancellationToken.None));

            Assert.AreEqual(unknownAssetId, exception.Id);
            Assert.AreEqual(1, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Asset_Is_In_Different_Store()
        {
            var assetStoreId = Guid.NewGuid();
            var currentUserStoreId = Guid.NewGuid();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(assetStoreId)
                .Build();

            _assetRepository.Seed(asset);

            _userAccessScopeResolver.SetStoreScope(currentUserStoreId);

            var exception = await Assert.ThrowsExactlyAsync<AssetNotFoundException>(() => _handler.HandleAsync(asset.Id, CancellationToken.None));

            Assert.AreEqual(asset.Id, exception.Id);
            Assert.AreEqual(0, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(1, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(asset.Id, _assetRepository.LastRequestedAssetId);
            Assert.AreEqual(currentUserStoreId, _assetRepository.LastRequestedStoreId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Scope_Has_No_StoreId()
        {
            _userAccessScopeResolver.SetScope(new UserAccessScope(CanAccessAllStores: false, StoreId: null));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _handler.HandleAsync(Guid.NewGuid(), CancellationToken.None));

            Assert.AreEqual(1, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_For_Global_Scope()
        {
            var asset = new AssetBuilder().Build();

            _assetRepository.Seed(asset);
            _userAccessScopeResolver.SetGlobalScope();

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync( asset.Id, cancellationToken);

            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastGetByIdCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_For_Store_Scope()
        {
            var storeId = Guid.NewGuid();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(storeId)
                .Build();

            _assetRepository.Seed(asset);

            _userAccessScopeResolver.SetStoreScope(storeId);

            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(asset.Id, cancellationToken);

            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastGetByIdAndStoreIdCancellationToken);
        }
    }
}