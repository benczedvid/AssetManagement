using AssetManagement.Application.Assets;
using AssetManagement.Application.Assets.UpdateAsset;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Stores;
using AssetManagement.Application.Users;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Assets.UpdateAsset
{
    [TestClass]
    public sealed class UpdateAssetHandlerTests
    {
        private FakeAssetRepository _assetRepository = null!;
        private FakeStoreRepository _storeRepository = null!;
        private FakeApplicationUserRepository _userRepository = null!;
        private FakeUserAccessScopeResolver _userAccessScopeResolver = null!;
        private UpdateAssetHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _assetRepository = new FakeAssetRepository();
            _storeRepository = new FakeStoreRepository();
            _userRepository = new FakeApplicationUserRepository();
            _userAccessScopeResolver = new FakeUserAccessScopeResolver();
            _handler = new UpdateAssetHandler(
                _assetRepository,
                _storeRepository,
                _userRepository,
                _userAccessScopeResolver);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetRepository_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new UpdateAssetHandler(
                        null!,
                        _storeRepository,
                        _userRepository,
                        _userAccessScopeResolver));

            Assert.AreEqual("assetRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_StoreRepository_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new UpdateAssetHandler(
                        _assetRepository,
                        null!,
                        _userRepository,
                        _userAccessScopeResolver));

            Assert.AreEqual("storeRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_UserRepository_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new UpdateAssetHandler(
                        _assetRepository,
                        _storeRepository,
                        null!,
                        _userAccessScopeResolver));

            Assert.AreEqual("userRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AccessScopeResolver_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new UpdateAssetHandler(
                        _assetRepository,
                        _storeRepository,
                        _userRepository,
                        null!));

            Assert.AreEqual("userAccessScopeResolver", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Id_Is_Empty()
        {
            var request = CreateValidRequest(assignedStoreId: Guid.NewGuid());

            var exception =
                await Assert.ThrowsExactlyAsync<
                    ArgumentException>(
                    () => _handler.HandleAsync(
                        Guid.Empty,
                        request,
                        CancellationToken.None));

            Assert.AreEqual("id", exception.ParamName);
            Assert.AreEqual(0, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(0, _assetRepository.GetForUpdateByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Request_Is_Null()
        {
            var exception =
                await Assert.ThrowsExactlyAsync<
                    ArgumentNullException>(
                    () => _handler.HandleAsync(
                        Guid.NewGuid(),
                        null!,
                        CancellationToken.None));

            Assert.AreEqual("request", exception.ParamName);
            Assert.AreEqual(0, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(0, _assetRepository.GetForUpdateByIdCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Update_Asset_For_Global_Scope()
        {
            var originalStore = new StoreBuilder()
                .WithStoreNumber("321")
                .Build();

            var targetStore = new StoreBuilder()
                .WithStoreNumber("322")
                .Build();

            var assignedUser = new ApplicationUserBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssetName("Original asset")
                .WithManufacturer("Original manufacturer")
                .WithModel("Original model")
                .WithSerialNumber("IMMUTABLE-SERIAL")
                .WithAssignedStoreId(originalStore.Id)
                .WithAssignedUserId(null)
                .WithAssignedVendorId(null)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(originalStore);
            _storeRepository.Seed(targetStore);
            _userRepository.Seed(assignedUser);

            _userAccessScopeResolver.SetGlobalScope();

            var vendorId = Guid.NewGuid();

            var request = CreateValidRequest(
                assignedStoreId: targetStore.Id,
                assignedUserId: assignedUser.Id,
                assignedVendorId: vendorId);

            var response = await _handler.HandleAsync(asset.Id, request, CancellationToken.None);

            Assert.AreEqual(asset.Id, response.AssetId);
            Assert.AreEqual(request.AssetName, response.AssetName);
            Assert.AreEqual(request.AssetType, response.AssetType);
            Assert.AreEqual(request.AssetStatus, response.AssetStatus);
            Assert.AreEqual(request.Manufacturer, response.Manufacturer);
            Assert.AreEqual(request.Model, response.Model);
            Assert.AreEqual( "IMMUTABLE-SERIAL", response.SerialNumber);
            Assert.AreEqual(request.MacAddress, response.MacAddress);
            Assert.AreEqual(request.WifiMacAddress, response.WifiMacAddress);
            Assert.AreEqual(request.Imei, response.Imei);
            Assert.AreEqual(request.OperatingSystem, response.OperatingSystem);
            Assert.AreEqual(request.OperatingSystemVersion, response.OperatingSystemVersion);
            Assert.AreEqual(request.AssignedStoreId, response.AssignedStoreId);
            Assert.AreEqual(request.AssignedUserId, response.AssignedUserId);
            Assert.AreEqual(request.AssignedVendorId, response.AssignedVendorId);
            Assert.AreEqual(request.RfIdTagId, response.RfidTagId);
            Assert.AreEqual(1, _assetRepository.GetForUpdateByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(1, _userRepository.GetByIdCallCount);
            Assert.AreEqual(1, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Update_Asset_For_Matching_Store_Scope()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedUserId(null)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);

            _userAccessScopeResolver.SetStoreScope(store.Id);

            var request = CreateValidRequest(
                assignedStoreId: store.Id,
                assignedUserId: null);

            var response = await _handler.HandleAsync(asset.Id, request, CancellationToken.None);

            Assert.AreEqual(asset.Id, response.AssetId);
            Assert.AreEqual(store.Id, response.AssignedStoreId);
            Assert.AreEqual(0, _assetRepository.GetForUpdateByIdCallCount);
            Assert.AreEqual(1, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(asset.Id, _assetRepository.LastRequestedAssetId);
            Assert.AreEqual(store.Id, _assetRepository.LastRequestedStoreId);
            Assert.AreEqual(1, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Trim_Updated_Text_Values()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedUserId(null)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);

            _userAccessScopeResolver.SetGlobalScope();

            var request = CreateValidRequest(assignedStoreId: store.Id) with
            {
                AssetName = "  Updated asset  ",
                Manufacturer = "  Lenovo  ",
                Model = "  ThinkPad T14  ",
                MacAddress = "  A1:B2:C3:D4:E5:F6  ",
                WifiMacAddress = "  F6:E5:D4:C3:B2:A1  ",
                Imei = "  123456789123456  ",
                OperatingSystem = "  Windows  ",
                OperatingSystemVersion = "  24H2  ",
                RfIdTagId = "  RFID-001  "
            };
            
            var response = await _handler.HandleAsync(asset.Id, request, CancellationToken.None);

            Assert.AreEqual("Updated asset", response.AssetName);
            Assert.AreEqual("Lenovo", response.Manufacturer);
            Assert.AreEqual("ThinkPad T14", response.Model);
            Assert.AreEqual("A1:B2:C3:D4:E5:F6", response.MacAddress);
            Assert.AreEqual("F6:E5:D4:C3:B2:A1", response.WifiMacAddress);
            Assert.AreEqual("123456789123456", response.Imei);
            Assert.AreEqual("Windows", response.OperatingSystem);
            Assert.AreEqual("24H2", response.OperatingSystemVersion);
            Assert.AreEqual("RFID-001", response.RfidTagId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Allow_Null_Optional_Values()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedUserId(Guid.NewGuid())
                .WithAssignedVendorId(Guid.NewGuid())
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);

            _userAccessScopeResolver.SetGlobalScope();

            var request = CreateValidRequest(assignedStoreId: store.Id) with
            {
                AssignedUserId = null,
                AssignedVendorId = null,
                MacAddress = null,
                WifiMacAddress = null,
                Imei = null,
                OperatingSystem = null,
                OperatingSystemVersion = null,
                RfIdTagId = null
            };

            var response = await _handler.HandleAsync(asset.Id, request, CancellationToken.None);

            Assert.IsNull(response.AssignedUserId);
            Assert.IsNull(response.AssignedVendorId);
            Assert.IsNull(response.MacAddress);
            Assert.IsNull(response.WifiMacAddress);
            Assert.IsNull(response.Imei);
            Assert.IsNull(response.OperatingSystem);
            Assert.IsNull(response.OperatingSystemVersion);
            Assert.IsNull(response.RfidTagId);
            Assert.AreEqual(0, _userRepository.GetByIdCallCount);
            Assert.AreEqual(1, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Assign_Vendor_When_VendorId_Is_Provided()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedVendorId(null)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);
            _userAccessScopeResolver.SetGlobalScope();

            var vendorId = Guid.NewGuid();

            var request = CreateValidRequest(assignedStoreId: store.Id, assignedVendorId: vendorId);

            var response = await _handler.HandleAsync(asset.Id, request, CancellationToken.None);

            Assert.AreEqual(vendorId, asset.AssignedVendorId);
            Assert.AreEqual(vendorId, response.AssignedVendorId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Remove_Vendor_When_VendorId_Is_Null()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedVendorId(Guid.NewGuid())
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);
            _userAccessScopeResolver.SetGlobalScope();

            var request = CreateValidRequest(assignedStoreId: store.Id, assignedVendorId: null);

            var response = await _handler.HandleAsync(asset.Id, request, CancellationToken.None);

            Assert.IsNull(asset.AssignedVendorId);
            Assert.IsNull(response.AssignedVendorId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Not_Change_SerialNumber()
        {
            const string originalSerialNumber = "SERIAL-001";

            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithSerialNumber(originalSerialNumber)
                .WithAssignedStoreId(store.Id)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);
            _userAccessScopeResolver.SetGlobalScope();

            var request = CreateValidRequest(assignedStoreId: store.Id);
            var response = await _handler.HandleAsync(asset.Id, request, CancellationToken.None);

            Assert.AreEqual(originalSerialNumber, asset.SerialNumber);
            Assert.AreEqual(originalSerialNumber, response.SerialNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Asset_Does_Not_Exist_For_Global_Scope()
        {
            var assetId = Guid.NewGuid();

            _userAccessScopeResolver.SetGlobalScope();

            var request = CreateValidRequest(assignedStoreId: Guid.NewGuid());

            var exception = await Assert.ThrowsExactlyAsync<
                    AssetNotFoundException>(
                    () => _handler.HandleAsync(
                        assetId,
                        request,
                        CancellationToken.None));

            Assert.AreEqual(assetId, exception.Id);
            Assert.AreEqual(1, _assetRepository.GetForUpdateByIdCallCount);
            Assert.AreEqual(0, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(0, _userRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
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

            var request = CreateValidRequest(assignedStoreId: currentUserStoreId);

            var exception = await Assert.ThrowsExactlyAsync<
                    AssetNotFoundException>(
                    () => _handler.HandleAsync(
                        asset.Id,
                        request,
                        CancellationToken.None));

            Assert.AreEqual(asset.Id, exception.Id);
            Assert.AreEqual(0, _assetRepository.GetForUpdateByIdCallCount);
            Assert.AreEqual(1, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(0, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Scope_Has_No_StoreId()
        {
            _userAccessScopeResolver.SetScope(new UserAccessScope(CanAccessAllStores: false, StoreId: null));

            var request = CreateValidRequest(assignedStoreId: Guid.NewGuid());

            var exception =  await Assert.ThrowsExactlyAsync<
                    InvalidOperationException>(
                    () => _handler.HandleAsync(
                        Guid.NewGuid(),
                        request,
                        CancellationToken.None));

            Assert.AreEqual("The current user does not have a valid store assignment.", exception.Message);
            Assert.AreEqual(0, _assetRepository.GetForUpdateByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Target_Store_Does_Not_Exist()
        {
            var originalStoreId = Guid.NewGuid();

            var asset = new AssetBuilder()
                .WithAssetName("Original asset")
                .WithAssignedStoreId(originalStoreId)
                .Build();

            _assetRepository.Seed(asset);
            _userAccessScopeResolver.SetGlobalScope();

            var unknownStoreId = Guid.NewGuid();

            var request = CreateValidRequest(assignedStoreId: unknownStoreId);

            await Assert.ThrowsExactlyAsync<StoreNotFoundException>(
                () => _handler.HandleAsync(
                    asset.Id,
                    request,
                    CancellationToken.None));

            Assert.AreEqual("Original asset", asset.AssetName);
            Assert.AreEqual(originalStoreId, asset.AssignedStoreId);
            Assert.AreEqual(0, _userRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Assigned_User_Does_Not_Exist()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssetName("Original asset")
                .WithAssignedStoreId(store.Id)
                .WithAssignedUserId(null)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);
            _userAccessScopeResolver.SetGlobalScope();

            var request = CreateValidRequest(
                assignedStoreId: store.Id,
                assignedUserId: Guid.NewGuid());

            await Assert.ThrowsExactlyAsync<ApplicationUserNotFoundException>(
                () => _handler.HandleAsync(
                    asset.Id,
                    request,
                    CancellationToken.None));

            Assert.AreEqual("Original asset", asset.AssetName);
            Assert.IsNull(asset.AssignedUserId);
            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(1, _userRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Propagate_Domain_Validation_Error()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssetName("Original asset")
                .WithAssignedStoreId(store.Id)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);
            _userAccessScopeResolver.SetGlobalScope();

            var request = CreateValidRequest(assignedStoreId: store.Id) with
            {
                AssetName = " "
            };

            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                    () => _handler.HandleAsync(
                        asset.Id,
                        request,
                        CancellationToken.None));

            Assert.AreEqual("assetName", exception.ParamName);
            Assert.AreEqual("Original asset", asset.AssetName);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_For_Global_Scope()
        {
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();
            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);
            _userRepository.Seed(user);

            _userAccessScopeResolver.SetGlobalScope();

            var request = CreateValidRequest(assignedStoreId: store.Id, assignedUserId: user.Id);

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(asset.Id, request, cancellationToken);

            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastGetForUpdateByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _storeRepository.LastGetByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _userRepository.LastGetByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastSaveChangesCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_For_Store_Scope()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedUserId(null)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);

            _userAccessScopeResolver.SetStoreScope(store.Id);

            var request = CreateValidRequest(assignedStoreId: store.Id, assignedUserId: null);

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(asset.Id, request, cancellationToken);

            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastGetByIdAndStoreIdCancellationToken);
            Assert.AreEqual(cancellationToken, _storeRepository.LastGetByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastSaveChangesCancellationToken);
        }

        private static UpdateAssetRequest CreateValidRequest(
            Guid assignedStoreId,
            Guid? assignedUserId = null,
            Guid? assignedVendorId = null)
        {
            return new UpdateAssetRequest(
                AssetName: "HUHQLAP9999",
                AssetType: AssetType.Notebook,
                AssetStatus: AssetStatus.In_HQ,
                Manufacturer: "Lenovo",
                Model: "ThinkPad T14",
                MacAddress: "74:5D:22:3A:76:C7",
                WifiMacAddress: "00:00:00:00:00:00",
                Imei: "123456789123456",
                OperatingSystem: "Windows",
                OperatingSystemVersion: "24H2",
                AssignedStoreId: assignedStoreId,
                AssignedUserId: assignedUserId,
                AssignedVendorId: assignedVendorId,
                RfIdTagId: "RFID-HUHQLAP9999");
        }
    }
}