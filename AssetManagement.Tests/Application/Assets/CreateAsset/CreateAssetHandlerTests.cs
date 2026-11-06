using AssetManagement.Application.Assets;
using AssetManagement.Application.Assets.CreateAsset;
using AssetManagement.Application.Stores;
using AssetManagement.Application.Users;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Assets.CreateAsset
{
    [TestClass]
    public sealed class CreateAssetHandlerTests
    {
        private FakeAssetRepository _assetRepository = null!;
        private FakeStoreRepository _storeRepository = null!;
        private FakeApplicationUserRepository _userRepository = null!;
        private CreateAssetHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _assetRepository = new FakeAssetRepository();
            _storeRepository = new FakeStoreRepository();
            _userRepository = new FakeApplicationUserRepository();
            _handler = new CreateAssetHandler(_assetRepository, _storeRepository, _userRepository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetRepository_Is_Null()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new CreateAssetHandler(null!, _storeRepository, _userRepository));
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_StoreRepository_Is_Null()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new CreateAssetHandler(_assetRepository, null!, _userRepository));
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_UserRepository_Is_Null()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new CreateAssetHandler(_assetRepository, _storeRepository, null!));
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Request_Is_Null()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _handler.HandleAsync(null!, CancellationToken.None));
            Assert.AreEqual(0, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(0, _userRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.AddCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Create_Asset_When_Request_Is_Valid()
        {
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();
            var vendorId = Guid.NewGuid();

            _storeRepository.Seed(store);
            _userRepository.Seed(user);

            var request = CreateValidRequest(
                assignedStoreId: store.Id,
                assignedUserId: user.Id,
                assignedVendorId: vendorId);

            var beforeCreation = DateTime.UtcNow;

            var response = await _handler.HandleAsync(request, CancellationToken.None);

            var afterCreation = DateTime.UtcNow;

            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(1, _userRepository.GetByIdCallCount);
            Assert.AreEqual(1, _assetRepository.AddCallCount);
            Assert.AreEqual(1, _assetRepository.SaveChangesCallCount);
            Assert.HasCount(1, _assetRepository.Assets);
            Assert.AreNotEqual(Guid.Empty, response.AssetId);
            Assert.AreEqual(request.AssetName, response.AssetName);
            Assert.AreEqual(request.AssetType, response.AssetType);
            Assert.AreEqual(request.AssetStatus, response.AssetStatus);
            Assert.AreEqual(request.Manufacturer, response.Manufacturer);
            Assert.AreEqual(request.Model, response.Model);
            Assert.AreEqual(request.SerialNumber, response.SerialNumber);
            Assert.AreEqual(request.AssignedStoreId, response.AssignedStoreId);
            Assert.AreEqual(request.AssignedUserId, response.AssignedUserId);
            Assert.AreEqual(request.AssignedVendorId, response.AssignedVendorId);
            Assert.AreEqual(request.AssignedEmployeeNumber, response.AssignedEmployeeNumber);
            Assert.AreEqual(request.RfidTagId, response.RfidTagId);
            Assert.AreEqual(request.MacAddress, response.MacAddress);
            Assert.AreEqual(request.WifimacAddress, response.WifimacAddress);
            Assert.AreEqual(request.Imei, response.Imei);
            Assert.AreEqual(request.OperatingSystem, response.OperatingSystem);
            Assert.AreEqual(request.OperatingSystemVersion, response.OperatingSystemVersion);

            Assert.IsGreaterThanOrEqualTo(beforeCreation, response.CreatedAtUtc);
            Assert.IsLessThanOrEqualTo(afterCreation, response.CreatedAtUtc);

            var createdAsset = _assetRepository.Assets.Single();

            Assert.AreEqual(response.AssetId, createdAsset.Id);
            Assert.AreEqual(response.CreatedAtUtc, createdAsset.CreatedAtUtc);
            Assert.AreEqual(store.Id, createdAsset.AssignedStoreId);
            Assert.AreEqual(user.Id, createdAsset.AssignedUserId);
            Assert.AreEqual(vendorId, createdAsset.AssignedVendorId);
            Assert.AreEqual(request.SerialNumber, createdAsset.SerialNumber);
            Assert.AreEqual(request.RfidTagId, createdAsset.RfidTagId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Create_Asset_Without_Optional_Assignments()
        {
            var store = new StoreBuilder().Build();

            _storeRepository.Seed(store);

            var request = CreateValidRequest(
                assignedStoreId: store.Id,
                assignedUserId: null,
                assignedVendorId: null) with
            {
                AssignedEmployeeNumber = null,
                RfidTagId = null,
                MacAddress = null,
                WifimacAddress = null,
                Imei = null,
                OperatingSystem = null,
                OperatingSystemVersion = null
            };

            var response = await _handler.HandleAsync(request, CancellationToken.None);

            Assert.AreEqual(0, _userRepository.GetByIdCallCount);
            Assert.IsNull(response.AssignedUserId);
            Assert.IsNull(response.AssignedVendorId);
            Assert.IsNull(response.AssignedEmployeeNumber);
            Assert.IsNull(response.RfidTagId);
            Assert.IsNull(response.MacAddress);
            Assert.IsNull(response.WifimacAddress);
            Assert.IsNull(response.Imei);
            Assert.IsNull(response.OperatingSystem);
            Assert.IsNull(response.OperatingSystemVersion);

            Assert.AreEqual(1, _assetRepository.AddCallCount);

            Assert.AreEqual(1, _assetRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken()
        {
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            _storeRepository.Seed(store);
            _userRepository.Seed(user);

            var request = CreateValidRequest(assignedStoreId: store.Id, assignedUserId: user.Id);

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(request, cancellationToken);

            Assert.AreEqual(cancellationToken, _storeRepository.LastGetByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _userRepository.LastGetByIdCancellationToken);
            Assert.AreEqual(cancellationToken,  _assetRepository.LastGetBySerialNumberCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastAddCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastSaveChangesCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Assigned_Store_Does_Not_Exist()
        {
            var user = new ApplicationUserBuilder().Build();

            _userRepository.Seed(user);
            
            var request = CreateValidRequest(assignedStoreId: Guid.NewGuid(), assignedUserId: user.Id);

            await Assert.ThrowsExactlyAsync<StoreNotFoundException>(() => _handler.HandleAsync(request, CancellationToken.None));

            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(0, _userRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.AddCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
            Assert.IsEmpty(_assetRepository.Assets);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Assigned_User_Does_Not_Exist()
        {
            var store = new StoreBuilder().Build();

            _storeRepository.Seed(store);

            var request = CreateValidRequest(assignedStoreId: store.Id, assignedUserId: Guid.NewGuid());

            await Assert.ThrowsExactlyAsync<ApplicationUserNotFoundException>(() => _handler.HandleAsync(request, CancellationToken.None));

            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(1, _userRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.AddCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
            Assert.IsEmpty(_assetRepository.Assets);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_SerialNumber_Already_Exists()
        {
            var store = new StoreBuilder().Build();
            var user = new ApplicationUserBuilder().Build();

            var existingAsset = new AssetBuilder()
                .WithSerialNumber("PF4KVG84")
                .WithAssignedStoreId(store.Id)
                .Build();

            _storeRepository.Seed(store);
            _userRepository.Seed(user);
            _assetRepository.Seed(existingAsset);

            var request = CreateValidRequest(
                assignedStoreId: store.Id,
                assignedUserId: user.Id);

            await Assert.ThrowsExactlyAsync<SerialNumberAlreadyExistsException>(() => _handler.HandleAsync(request, CancellationToken.None));

            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(1, _userRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.AddCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
            Assert.HasCount(1, _assetRepository.Assets);
            Assert.AreSame(existingAsset, _assetRepository.Assets.Single());
        }

        [TestMethod]
        public async Task HandleAsync_Should_Propagate_Domain_Validation_Error()
        {
            var store = new StoreBuilder().Build();

            _storeRepository.Seed(store);

            var request = CreateValidRequest(
                assignedStoreId: store.Id,
                assignedUserId: null) with
            {
                AssetName = " "
            };

            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(() => _handler.HandleAsync(request, CancellationToken.None));

            Assert.AreEqual("assetName",exception.ParamName);
            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(0, _userRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.AddCallCount);
            Assert.AreEqual(0, _assetRepository.SaveChangesCallCount);
        }

        private static CreateAssetRequest CreateValidRequest(
            Guid assignedStoreId,
            Guid? assignedUserId,
            Guid? assignedVendorId = null)
        {
            return new CreateAssetRequest(
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
                AssignedStoreId: assignedStoreId,
                AssignedUserId: assignedUserId,
                AssignedVendorId: assignedVendorId,
                RfidTagId: "RFID-PF4KVG84");
        }
    }
}