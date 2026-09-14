using AssetManagement.Application.Assets.GetAssets;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Assets.GetAssets
{
    [TestClass]
    public sealed class GetAssetsHandlerTests
    {
        private FakeAssetRepository _assetRepository = null!;
        private FakeEmployeeRepository _employeeRepository = null!;
        private FakeUserAccessScopeResolver _userAccessScopeResolver = null!;
        private GetAssetsHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _assetRepository = new FakeAssetRepository();
            _employeeRepository = new FakeEmployeeRepository();
            _userAccessScopeResolver = new FakeUserAccessScopeResolver();
            _handler = new GetAssetsHandler(_assetRepository, _employeeRepository, _userAccessScopeResolver);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetRepository_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetAssetsHandler(
                        null!,
                        _employeeRepository,
                        _userAccessScopeResolver));

            Assert.AreEqual("assetRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_EmployeeRepository_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetAssetsHandler(
                        _assetRepository,
                        null!,
                        _userAccessScopeResolver));

            Assert.AreEqual("employeeRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_UserAccessScopeResolver_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetAssetsHandler(
                        _assetRepository,
                        _employeeRepository,
                        null!));

            Assert.AreEqual("userAccessScopeResolver", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_All_Assets_For_Global_Scope()
        {
            var firstAsset = new AssetBuilder()
                .WithAssetName("HUHQLAP0287")
                .WithSerialNumber("PF4KVG84")
                .WithAssignedEmployeeNumber(null)
                .Build();

            var secondAsset = new AssetBuilder()
                .WithAssetName("HUHQLAP0288")
                .WithSerialNumber("PF4KVG85")
                .WithAssignedEmployeeNumber(null)
                .Build();

            _assetRepository.Seed(firstAsset);
            _assetRepository.Seed(secondAsset);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.HasCount(2, response);
            Assert.AreEqual(1, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(1, _assetRepository.GetAllCallCount);
            Assert.AreEqual(0, _assetRepository.GetAllByStoreIdCallCount);
            Assert.IsTrue(response.Any(item => item.AssetId == firstAsset.Id));
            Assert.IsTrue(response.Any(item => item.AssetId == secondAsset.Id));
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Only_Store_Assets_For_Store_Scope()
        {
            var accessibleStoreId = Guid.NewGuid();
            var otherStoreId = Guid.NewGuid();

            var accessibleAsset = new AssetBuilder()
                .WithAssetName("Accessible asset")
                .WithSerialNumber("SERIAL-001")
                .WithAssignedStoreId(accessibleStoreId)
                .WithAssignedEmployeeNumber(null)
                .Build();

            var otherAsset = new AssetBuilder()
                .WithAssetName("Other asset")
                .WithSerialNumber("SERIAL-002")
                .WithAssignedStoreId(otherStoreId)
                .WithAssignedEmployeeNumber(null)
                .Build();

            _assetRepository.Seed(accessibleAsset);
            _assetRepository.Seed(otherAsset);

            _userAccessScopeResolver.SetStoreScope(accessibleStoreId);

            var response = await _handler.HandleAsync( CancellationToken.None);

            Assert.HasCount(1, response);
            Assert.AreEqual(accessibleAsset.Id, response[0].AssetId);
            Assert.AreEqual(accessibleStoreId, response[0].AssignedStoreId);
            Assert.AreEqual(0, _assetRepository.GetAllCallCount);
            Assert.AreEqual(1, _assetRepository.GetAllByStoreIdCallCount);
            Assert.AreEqual(accessibleStoreId, _assetRepository.LastRequestedStoreId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Scope_Has_No_StoreId()
        {
            _userAccessScopeResolver.SetScope(new UserAccessScope(CanAccessAllStores: false, StoreId: null));

            var exception =
                await Assert.ThrowsExactlyAsync<
                    InvalidOperationException>(
                    () => _handler.HandleAsync(
                        CancellationToken.None));

            Assert.AreEqual("The current user does not have a valid store assignment.", exception.Message);
            Assert.AreEqual(1, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(0, _assetRepository.GetAllCallCount);
            Assert.AreEqual(0, _assetRepository.GetAllByStoreIdCallCount);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumbersCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Empty_List_When_No_Assets_Exist()
        {
            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.IsEmpty(response);
            Assert.AreEqual(1, _assetRepository.GetAllCallCount);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumbersCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Map_All_Asset_Properties()
        {
            var assignedStoreId = Guid.NewGuid();
            var assignedUserId = Guid.NewGuid();
            var assignedVendorId = Guid.NewGuid();

            var asset = new AssetBuilder()
                .WithAssetName("HUHQLAP0287")
                .WithAssetType(AssetType.Notebook)
                .WithAssetStatus(AssetStatus.Used_In_HQ)
                .WithManufacturer("Lenovo")
                .WithModel("21JN0008HV")
                .WithSerialNumber("PF4KVG84")
                .WithAssignedStoreId(assignedStoreId)
                .WithAssignedUserId(assignedUserId)
                .WithAssignedVendorId(assignedVendorId)
                .WithAssignedEmployeeNumber(null)
                .WithRfidTagId("RFID-PF4KVG84")
                .WithMacAddress("74:5D:22:3A:76:C7")
                .WithWiFiMacAddress("00:00:00:00:00:00")
                .WithImei("123456789123456")
                .WithOperatingSystem("Windows")
                .WithOperatingSystemVersion("24H2")
                .Build();

            _assetRepository.Seed(asset);
            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(CancellationToken.None);

            var result = response.Single();

            Assert.AreEqual(asset.Id, result.AssetId);
            Assert.AreEqual(asset.AssetName, result.AssetName);
            Assert.AreEqual(asset.AssetType, result.AssetType);
            Assert.AreEqual(asset.AssetStatus, result.AssetStatus);
            Assert.AreEqual(asset.Manufacturer, result.Manufacturer);
            Assert.AreEqual(asset.Model, result.Model);
            Assert.AreEqual(asset.SerialNumber, result.SerialNumber);
            Assert.AreEqual(asset.MacAddress, result.MacAddress);
            Assert.AreEqual(asset.WiFiMacAddress, result.WifiMacAddress);
            Assert.AreEqual(asset.Imei, result.Imei);
            Assert.AreEqual(asset.OperatingSystem, result.OperatingSystem);
            Assert.AreEqual(asset.OperatingSystemVersion, result.OperatingSystemVersion);
            Assert.AreEqual(asset.AssignedStoreId, result.AssignedStoreId);
            Assert.AreEqual(asset.AssignedUserId, result.AssignedUserId);
            Assert.AreEqual(asset.CreatedAtUtc, result.CreatedAtUtc);
            Assert.AreEqual(asset.AssignedVendorId, result.AssignedVendorId);
            Assert.AreEqual(asset.RfidTagId, result.RfidTagId);
            Assert.IsNull(result.AssignedEmployeeName);
            Assert.IsNull(result.AssignedVendorName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Assigned_Employee_Name()
        {
            const string employeeNumber = "103549";
            const string employeeName = "John Doe";

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithFullName(employeeName)
                .Build();

            var asset = new AssetBuilder()
                .WithAssignedEmployeeNumber(employeeNumber)
                .Build();

            _employeeRepository.Seed(employee);
            _assetRepository.Seed(asset);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(
                CancellationToken.None);

            var result = response.Single();

            Assert.AreEqual(employeeName, result.AssignedEmployeeName);
            Assert.AreEqual(1, _employeeRepository.GetByEmployeeNumbersCallCount);
            Assert.HasCount(1, _employeeRepository.LastRequestedEmployeeNumbers);
            Assert.AreEqual(employeeNumber, _employeeRepository.LastRequestedEmployeeNumbers.Single());
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Null_Employee_Name_When_Employee_Does_Not_Exist()
        {
            var asset = new AssetBuilder()
                .WithAssignedEmployeeNumber("999999")
                .Build();

            _assetRepository.Seed(asset);
            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(CancellationToken.None);

            var result = response.Single();

            Assert.IsNull(result.AssignedEmployeeName);
            Assert.AreEqual(1, _employeeRepository.GetByEmployeeNumbersCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Not_Request_Employees_When_Assets_Have_No_Employee_Number()
        {
            var firstAsset = new AssetBuilder()
                .WithAssetName("Asset A")
                .WithSerialNumber("SERIAL-A")
                .WithAssignedEmployeeNumber(null)
                .Build();

            var secondAsset = new AssetBuilder()
                .WithAssetName("Asset B")
                .WithSerialNumber("SERIAL-B")
                .WithAssignedEmployeeNumber(null)
                .Build();

            _assetRepository.Seed(firstAsset);
            _assetRepository.Seed(secondAsset);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.HasCount(2, response);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumbersCallCount);
            Assert.IsTrue(response.All(item => item.AssignedEmployeeName is null));
        }

        [TestMethod]
        public async Task HandleAsync_Should_Request_Each_Employee_Number_Only_Once()
        {
            const string employeeNumber = "103549";

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithFullName("John Doe")
                .Build();

            var firstAsset = new AssetBuilder()
                .WithAssetName("Asset A")
                .WithSerialNumber("SERIAL-A")
                .WithAssignedEmployeeNumber(employeeNumber)
                .Build();

            var secondAsset = new AssetBuilder()
                .WithAssetName("Asset B")
                .WithSerialNumber("SERIAL-B")
                .WithAssignedEmployeeNumber(employeeNumber)
                .Build();

            _employeeRepository.Seed(employee);
            _assetRepository.Seed(firstAsset);
            _assetRepository.Seed(secondAsset);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.HasCount(2, response);
            Assert.AreEqual(1, _employeeRepository.GetByEmployeeNumbersCallCount);
            Assert.HasCount(1, _employeeRepository.LastRequestedEmployeeNumbers);
            Assert.AreEqual(employeeNumber, _employeeRepository.LastRequestedEmployeeNumbers.Single());
            Assert.IsTrue(response.All(item => item.AssignedEmployeeName == "John Doe"));
        }

        [TestMethod]
        public async Task HandleAsync_Should_Deduplicate_Employee_Numbers_Ignoring_Case()
        {
            const string storedEmployeeNumber = "EMPLOYEE01";

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(storedEmployeeNumber)
                .WithFullName("John Doe")
                .Build();

            var firstAsset = new AssetBuilder()
                .WithAssetName("Asset A")
                .WithSerialNumber("SERIAL-A")
                .WithAssignedEmployeeNumber("EMPLOYEE01")
                .Build();

            var secondAsset = new AssetBuilder()
                .WithAssetName("Asset B")
                .WithSerialNumber("SERIAL-B")
                .WithAssignedEmployeeNumber("employee01")
                .Build();

            _employeeRepository.Seed(employee);
            _assetRepository.Seed(firstAsset);
            _assetRepository.Seed(secondAsset);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.HasCount(1, _employeeRepository.LastRequestedEmployeeNumbers);
            Assert.AreEqual(storedEmployeeNumber, _employeeRepository.LastRequestedEmployeeNumbers.Single());
            Assert.IsTrue(response.All(item => item.AssignedEmployeeName == "John Doe"));
        }

        [TestMethod]
        public async Task HandleAsync_Should_Order_Assets_By_AssetName()
        {
            var thirdAsset = new AssetBuilder()
                .WithAssetName("Charlie")
                .WithSerialNumber("SERIAL-C")
                .WithAssignedEmployeeNumber(null)
                .Build();

            var firstAsset = new AssetBuilder()
                .WithAssetName("Alpha")
                .WithSerialNumber("SERIAL-A")
                .WithAssignedEmployeeNumber(null)
                .Build();

            var secondAsset = new AssetBuilder()
                .WithAssetName("Bravo")
                .WithSerialNumber("SERIAL-B")
                .WithAssignedEmployeeNumber(null)
                .Build();

            _assetRepository.Seed(thirdAsset);
            _assetRepository.Seed(firstAsset);
            _assetRepository.Seed(secondAsset);

            _userAccessScopeResolver.SetGlobalScope();

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.HasCount(3, response);
            Assert.AreEqual("Alpha", response[0].AssetName);
            Assert.AreEqual("Bravo", response[1].AssetName);
            Assert.AreEqual("Charlie", response[2].AssetName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_For_Global_Scope()
        {
            var asset = new AssetBuilder()
                .WithAssignedEmployeeNumber("103549")
                .Build();

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber("103549")
                .Build();

            _assetRepository.Seed(asset);
            _employeeRepository.Seed(employee);

            _userAccessScopeResolver.SetGlobalScope();

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(cancellationToken);

            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastGetAllCancellationToken);
            Assert.AreEqual(cancellationToken, _employeeRepository.LastGetByEmployeeNumbersCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_For_Store_Scope()
        {
            var storeId = Guid.NewGuid();

            var asset = new AssetBuilder()
                .WithAssignedStoreId(storeId)
                .WithAssignedEmployeeNumber("103549")
                .Build();

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber("103549")
                .Build();

            _assetRepository.Seed(asset);
            _employeeRepository.Seed(employee);

            _userAccessScopeResolver.SetStoreScope(storeId);

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(cancellationToken);

            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _assetRepository.LastGetAllByStoreIdCancellationToken);
            Assert.AreEqual(cancellationToken, _employeeRepository.LastGetByEmployeeNumbersCancellationToken);
        }
    }
}