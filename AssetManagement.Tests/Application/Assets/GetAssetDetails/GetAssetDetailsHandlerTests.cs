using AssetManagement.Application.Assets;
using AssetManagement.Application.Assets.GetAssetDetails;
using AssetManagement.Application.Employees;
using AssetManagement.Application.Stores;
using AssetManagement.Domain.Entities.AssetMovements;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Assets.GetAssetDetails
{
    [TestClass]
    public sealed class GetAssetDetailsHandlerTests
    {
        private FakeAssetRepository _assetRepository = null!;
        private FakeAssetMovementRepository _assetMovementRepository = null!;
        private FakeEmployeeRepository _employeeRepository = null!;
        private FakeStoreRepository  _storeRepository = null!;
        private FakeUserAccessScopeResolver _userAccessScopeResolver = null!;
        private GetAssetDetailsHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _assetRepository = new FakeAssetRepository();
            _assetMovementRepository = new FakeAssetMovementRepository();
            _employeeRepository = new FakeEmployeeRepository();
            _storeRepository = new FakeStoreRepository();
            _userAccessScopeResolver = new FakeUserAccessScopeResolver();

            _handler = new GetAssetDetailsHandler(
                _assetRepository,
                _assetMovementRepository,
                _employeeRepository,
                _storeRepository,
                _userAccessScopeResolver);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetRepository_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetAssetDetailsHandler(
                        null!,
                        _assetMovementRepository,
                        _employeeRepository,
                        _storeRepository,
                        _userAccessScopeResolver));

            Assert.AreEqual("assetRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetMovementRepository_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetAssetDetailsHandler(
                        _assetRepository,
                        null!,
                        _employeeRepository,
                        _storeRepository,
                        _userAccessScopeResolver));

            Assert.AreEqual("assetMovementRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_EmployeeRepository_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetAssetDetailsHandler(
                        _assetRepository,
                        _assetMovementRepository,
                        null!,
                        _storeRepository,
                        _userAccessScopeResolver));

            Assert.AreEqual("employeeRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_StoreRepository_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetAssetDetailsHandler(
                        _assetRepository,
                        _assetMovementRepository,
                        _employeeRepository,
                        null!,
                        _userAccessScopeResolver));

            Assert.AreEqual("storeRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_UserAccessScopeResolver_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetAssetDetailsHandler(
                        _assetRepository,
                        _assetMovementRepository,
                        _employeeRepository,
                        _storeRepository,
                        null!));

            Assert.AreEqual("userAccessScopeResolver", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_AssetId_Is_Empty()
        {
            var exception =
                await Assert.ThrowsExactlyAsync<
                    ArgumentException>(
                    () => _handler.HandleAsync(
                        Guid.Empty,
                        CancellationToken.None));

            Assert.AreEqual("assetId", exception.ParamName);
            Assert.AreEqual(0, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Asset_Details_For_Global_Scope()
        {
            var store = new StoreBuilder()
                .WithName("Budapest M3")
                .Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedUserId(null)
                .WithAssignedEmployeeNumber(null)
                .WithAssignedVendorId(Guid.NewGuid())
                .WithRfidTagId("RFID-0001")
                .Build();

            _storeRepository.Seed(store);
            _assetRepository.Seed(asset);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.AreEqual(asset.Id, response.AssetId);
            Assert.AreEqual(asset.AssetName, response.AssetName);
            Assert.AreEqual(asset.AssetType, response.AssetType);
            Assert.AreEqual(asset.AssetStatus, response.AssetStatus);
            Assert.AreEqual(asset.Manufacturer, response.Manufacturer);
            Assert.AreEqual(asset.Model, response.Model);
            Assert.AreEqual(asset.SerialNumber, response.SerialNumber);
            Assert.AreEqual(asset.MacAddress, response.MacAddress);
            Assert.AreEqual(asset.WiFiMacAddress, response.WifiMacAddress);
            Assert.AreEqual(asset.Imei, response.Imei);
            Assert.AreEqual(asset.OperatingSystem, response.OperatingSystem);
            Assert.AreEqual(asset.OperatingSystemVersion, response.OperatingSystemVersion);
            Assert.AreEqual(asset.AssignedStoreId, response.AssignedStoreId);
            Assert.AreEqual(store.Name, response.AssignedStoreName);
            Assert.IsNull(response.AssignedEmployeeNumber);
            Assert.IsNull(response.AssignedEmployeeName);
            Assert.AreEqual(asset.CreatedAtUtc, response.CreatedAtUtc);
            Assert.AreEqual(asset.AssignedVendorId, response.AssignedVendorId);
            Assert.IsNull(response.AssignedVendorName);
            Assert.AreEqual(asset.RfidTagId, response.RfidTagId);
            Assert.IsEmpty(response.History);
            Assert.AreEqual(1, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(1, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(1, _assetMovementRepository.GetRecentByAssetIdCallCount);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumberCallCount);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumbersCallCount);
            Assert.AreEqual(0, _storeRepository.GetByIdsCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Asset_For_Matching_Store_Scope()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedEmployeeNumber(null)
                .Build();

            _storeRepository.Seed(store);
            _assetRepository.Seed(asset);

            _userAccessScopeResolver.SetStoreScope(store.Id);

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.AreEqual(asset.Id, response.AssetId);
            Assert.AreEqual(store.Id, response.AssignedStoreId);
            Assert.AreEqual(0, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(1, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(asset.Id, _assetRepository.LastRequestedAssetId);
            Assert.AreEqual(store.Id, _assetRepository.LastRequestedStoreId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Assigned_Employee_Data()
        {
            const string employeeNumber = "103549";
            const string employeeName = "John Doe";

            var store = new StoreBuilder().Build();

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithFullName(employeeName)
                .Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedEmployeeNumber(employeeNumber)
                .Build();

            _storeRepository.Seed(store);
            _employeeRepository.Seed(employee);
            _assetRepository.Seed(asset);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.AreEqual(employeeNumber, response.AssignedEmployeeNumber);
            Assert.AreEqual(employeeName, response.AssignedEmployeeName);
            Assert.AreEqual(1, _employeeRepository.GetByEmployeeNumberCallCount);
            Assert.AreEqual(employeeNumber, _employeeRepository.LastRequestedEmployeeNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Movement_History_With_Employee_And_Store_Names()
        {
            var assignedStore = new StoreBuilder()
                .WithStoreNumber("321")
                .WithName("Budapest M3")
                .Build();

            var movementStore = new StoreBuilder()
                .WithStoreNumber("322")
                .WithName("Budapest Váci út")
                .Build();

            const string employeeNumber = "103549";
            const string employeeName = "John Doe";

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithFullName(employeeName)
                .Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(assignedStore.Id)
                .WithAssignedEmployeeNumber(employeeNumber)
                .Build();

            var checkoutMovement = AssetMovement.Create(
                assetId: asset.Id,
                storeId: movementStore.Id,
                employeeNumber: employeeNumber,
                type: AssetMovementType.Checkout);

            var returnMovement = AssetMovement.Create(
                assetId: asset.Id,
                storeId: movementStore.Id,
                employeeNumber: employeeNumber,
                type: AssetMovementType.Return);

            _assetRepository.Seed(asset);

            _assetMovementRepository.Seed(checkoutMovement);
            _assetMovementRepository.Seed(returnMovement);
            _storeRepository.Seed(assignedStore);
            _storeRepository.Seed(movementStore);
            _employeeRepository.Seed(employee);
            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.HasCount(2, response.History);

            var checkoutHistoryItem = response.History.Single(item => item.Id == checkoutMovement.Id);

            Assert.AreEqual(AssetMovementType.Checkout, checkoutHistoryItem.Type);
            Assert.AreEqual(employeeNumber, checkoutHistoryItem.EmployeeNumber);
            Assert.AreEqual(employeeName, checkoutHistoryItem.EmployeeName);
            Assert.AreEqual(movementStore.Id, checkoutHistoryItem.StoreId);
            Assert.AreEqual(movementStore.Name, checkoutHistoryItem.StoreName);
            Assert.AreEqual(checkoutMovement.CreatedAtUtc, checkoutHistoryItem.CreatedAtUtc);

            var returnHistoryItem =
                response.History.Single(
                    item =>
                        item.Id == returnMovement.Id);

            Assert.AreEqual(AssetMovementType.Return, returnHistoryItem.Type);
            Assert.AreEqual(employeeName, returnHistoryItem.EmployeeName);
            Assert.AreEqual(movementStore.Name, returnHistoryItem.StoreName);
            Assert.AreEqual(1, _employeeRepository.GetByEmployeeNumbersCallCount);
            Assert.AreEqual(1, _storeRepository.GetByIdsCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_History_Ordered_By_CreatedAtUtc_Descending()
        {
            var store = new StoreBuilder().Build();

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber("103549")
                .Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedEmployeeNumber(null)
                .Build();

            var firstMovement = AssetMovement.Create(
                assetId: asset.Id,
                storeId: store.Id,
                employeeNumber: "103549",
                type: AssetMovementType.Checkout);

            Thread.Sleep(10);

            var secondMovement = AssetMovement.Create(
                assetId: asset.Id,
                storeId: store.Id,
                employeeNumber: "103549",
                type: AssetMovementType.Return);

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);
            _employeeRepository.Seed(employee);

            _assetMovementRepository.Seed(firstMovement);
            _assetMovementRepository.Seed(secondMovement);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.HasCount(2, response.History);
            Assert.AreEqual(secondMovement.Id, response.History[0].Id);
            Assert.AreEqual(firstMovement.Id, response.History[1].Id);
            Assert.IsGreaterThanOrEqualTo(response.History[1].CreatedAtUtc, response.History[0].CreatedAtUtc);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Unknown_Employee_When_History_Employee_Is_Not_Found()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedEmployeeNumber(null)
                .Build();

            var movement = AssetMovement.Create(
                assetId: asset.Id,
                storeId: store.Id,
                employeeNumber: "999999",
                type: AssetMovementType.Checkout);

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);
            _assetMovementRepository.Seed(movement);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(asset.Id, CancellationToken.None);

            Assert.HasCount(1, response.History);
            Assert.AreEqual("Unknown employee", response.History[0].EmployeeName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Unknown_Store_When_History_Store_Is_Not_Found()
        {
            var assignedStore = new StoreBuilder().Build();
            var unknownMovementStoreId = Guid.NewGuid();

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber("103549")
                .Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(assignedStore.Id)
                .WithAssignedEmployeeNumber(null)
                .Build();

            var movement = AssetMovement.Create(
                assetId: asset.Id,
                storeId: unknownMovementStoreId,
                employeeNumber: "103549",
                type: AssetMovementType.Checkout);

            _assetRepository.Seed(asset);
            _storeRepository.Seed(assignedStore);
            _employeeRepository.Seed(employee);
            _assetMovementRepository.Seed(movement);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(
                asset.Id,
                CancellationToken.None);

            Assert.HasCount(1, response.History);
            Assert.AreEqual("Unknown store", response.History[0].StoreName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Asset_Does_Not_Exist()
        {
            var assetId = Guid.NewGuid();

            _userAccessScopeResolver.SetGlobalScope();

            var exception =
                await Assert.ThrowsExactlyAsync<
                    AssetNotFoundException>(
                    () => _handler.HandleAsync(
                        assetId,
                        CancellationToken.None));

            Assert.AreEqual(assetId, exception.Id);
            Assert.AreEqual(0, _storeRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetMovementRepository.GetRecentByAssetIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Asset_Is_In_Different_Store()
        {
            var assetStore = new StoreBuilder().Build();
            var currentUserStoreId = Guid.NewGuid();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(assetStore.Id)
                .WithAssignedEmployeeNumber(null)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(assetStore);

            _userAccessScopeResolver.SetStoreScope(
                currentUserStoreId);

            var exception =
                await Assert.ThrowsExactlyAsync<
                    AssetNotFoundException>(
                    () => _handler.HandleAsync(
                        asset.Id,
                        CancellationToken.None));

            Assert.AreEqual(asset.Id, exception.Id);
            Assert.AreEqual(0, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(1, _assetRepository.GetByIdAndStoreIdCallCount);
            Assert.AreEqual(0, _storeRepository.GetByIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Scope_Has_No_StoreId()
        {
            _userAccessScopeResolver.SetScope(new(CanAccessAllStores: false, StoreId: null));

            await Assert.ThrowsExactlyAsync<
                InvalidOperationException>(
                () => _handler.HandleAsync(
                    Guid.NewGuid(),
                    CancellationToken.None));
           
            Assert.AreEqual(0, _assetRepository.GetByIdCallCount);
            Assert.AreEqual(0, _assetRepository.GetByIdAndStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Assigned_Store_Does_Not_Exist()
        {
            var asset = new AssetBuilder()
                .WithAssignedStoreId(Guid.NewGuid())
                .WithAssignedEmployeeNumber(null)
                .Build();

            _assetRepository.Seed(asset);
            _userAccessScopeResolver.SetGlobalScope();

            var exception =
                await Assert.ThrowsExactlyAsync<
                    StoreNotFoundException>(
                    () => _handler.HandleAsync(
                        asset.Id,
                        CancellationToken.None));

            Assert.AreEqual(0, _assetMovementRepository.GetRecentByAssetIdCallCount);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumberCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Assigned_Employee_Does_Not_Exist()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedEmployeeNumber("999999")
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);

            _userAccessScopeResolver.SetGlobalScope();

            await Assert.ThrowsExactlyAsync<
                EmployeeNotFoundException>(
                () => _handler.HandleAsync(
                    asset.Id,
                    CancellationToken.None));

            Assert.AreEqual(1, _employeeRepository.GetByEmployeeNumberCallCount);
            Assert.AreEqual(0, _assetMovementRepository.GetRecentByAssetIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken()
        {
            var store = new StoreBuilder().Build();

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber("103549")
                .Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedEmployeeNumber("103549")
                .Build();

            var movement = AssetMovement.Create(
                assetId: asset.Id,
                storeId: store.Id,
                employeeNumber: "103549",
                type: AssetMovementType.Checkout);

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);
            _employeeRepository.Seed(employee);
            _assetMovementRepository.Seed(movement);

            _userAccessScopeResolver.SetGlobalScope();

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(asset.Id, cancellationToken);

            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastGetByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _storeRepository.LastGetByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _employeeRepository.LastGetByEmployeeNumberCancellationToken);
            Assert.AreEqual(cancellationToken, _assetMovementRepository.LastGetRecentByAssetIdCancellationToken);
            Assert.AreEqual(cancellationToken, _employeeRepository.LastGetByEmployeeNumbersCancellationToken);
            Assert.AreEqual(cancellationToken, _storeRepository.LastGetByIdsCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Request_Movements_From_Last_Seven_Days()
        {
            var store = new StoreBuilder().Build();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(store.Id)
                .WithAssignedEmployeeNumber(null)
                .Build();

            _assetRepository.Seed(asset);
            _storeRepository.Seed(store);

            _userAccessScopeResolver.SetGlobalScope();

            var beforeRequest = DateTime.UtcNow.AddDays(-7);

            await _handler.HandleAsync(asset.Id, CancellationToken.None);

            var afterRequest = DateTime.UtcNow.AddDays(-7);

            Assert.IsNotNull(_assetMovementRepository.LastRequestedFromUtc);
            Assert.IsTrue(_assetMovementRepository.LastRequestedFromUtc >= beforeRequest);
            Assert.IsTrue(_assetMovementRepository.LastRequestedFromUtc <= afterRequest);
            Assert.AreEqual(asset.Id, _assetMovementRepository.LastRequestedAssetId);
        }
    }
}