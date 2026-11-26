using AssetManagement.Application.AssetMovements.CreateAssetMovement;
using AssetManagement.Application.AssetMovements.Exceptions;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Employees;
using AssetManagement.Domain.Entities.AssetMovements;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Assets.Exceptions;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.AssetMovements.CreateAssetMovement
{
    [TestClass]
    public sealed class CreateAssetMovementHandlerTests
    {
        private const string RfidTagId = "RFID-0001";
        private const string EmployeeNumber = "103549";

        private FakeAssetMovementRepository _assetMovementRepository = null!;
        private FakeAssetRepository _assetRepository = null!;
        private FakeEmployeeRepository _employeeRepository = null!;
        private CreateAssetMovementHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _assetMovementRepository = new FakeAssetMovementRepository();
            _assetRepository = new FakeAssetRepository();
            _employeeRepository = new FakeEmployeeRepository();

            _handler = new CreateAssetMovementHandler(
                _assetMovementRepository,
                _assetRepository,
                _employeeRepository);

            _employeeRepository.Seed(
                new EmployeeBuilder()
                    .WithEmployeeNumber(EmployeeNumber)
                    .Build());
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetMovementRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new CreateAssetMovementHandler(null!, _assetRepository, _employeeRepository));

            Assert.AreEqual("assetMovementRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new CreateAssetMovementHandler(_assetMovementRepository, null!, _employeeRepository));

            Assert.AreEqual("assetRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_EmployeeRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new CreateAssetMovementHandler(_assetMovementRepository, _assetRepository, null!));

            Assert.AreEqual("employeeRepository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Request_Is_Null()
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => _handler.HandleAsync(null!, CancellationToken.None));

            Assert.AreEqual("request", exception.ParamName);
            Assert.AreEqual(0, _assetRepository.GetByRfidTagIdCallCount);
            Assert.AreEqual(0, _assetMovementRepository.AddCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Check_Out_Pdt_Located_In_Store()
        {
            var asset = SeedAsset(new AssetBuilder().AsPdtInStore());

            var response = await _handler.HandleAsync(CreateRequest(), CancellationToken.None);

            Assert.AreEqual(AssetMovementType.Checkout, response.Type);
            Assert.AreEqual(AssetStatus.Used_In_Store, asset.AssetStatus);
            Assert.AreEqual(EmployeeNumber, asset.AssignedEmployeeNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Pdt_Checked_Out_By_Same_Employee()
        {
            var asset = SeedAsset(new AssetBuilder().AsCheckedOutPdt(EmployeeNumber));

            var response = await _handler.HandleAsync(CreateRequest(), CancellationToken.None);

            Assert.AreEqual(AssetMovementType.Return, response.Type);
            Assert.AreEqual(AssetStatus.In_Store, asset.AssetStatus);
            Assert.IsNull(asset.AssignedEmployeeNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Not_Return_Pdt_Checked_Out_By_Another_Employee()
        {
            var asset = SeedAsset(new AssetBuilder().AsCheckedOutPdt("999999"));

            await Assert.ThrowsExactlyAsync<AssetStateConflictException>(
                () => _handler.HandleAsync(CreateRequest(), CancellationToken.None));

            Assert.AreEqual(AssetStatus.Used_In_Store, asset.AssetStatus);
            Assert.AreEqual("999999", asset.AssignedEmployeeNumber);
            Assert.AreEqual(0, _assetMovementRepository.AddCallCount);
            Assert.AreEqual(0, _assetMovementRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Record_And_Save_Asset_Movement()
        {
            var asset = SeedAsset(new AssetBuilder().AsPdtInStore());

            await _handler.HandleAsync(CreateRequest(), CancellationToken.None);

            Assert.AreEqual(1, _assetMovementRepository.AddCallCount);
            Assert.AreEqual(1, _assetMovementRepository.SaveChangesCallCount);

            var movement = _assetMovementRepository.AssetMovements.Single();

            Assert.AreEqual(asset.Id, movement.AssetId);
            Assert.AreEqual(asset.AssignedStoreId, movement.StoreId);
            Assert.AreEqual(EmployeeNumber, movement.EmployeeNumber);
            Assert.AreEqual(AssetMovementType.Checkout, movement.Type);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Created_Movement()
        {
            var asset = SeedAsset(
                new AssetBuilder()
                    .AsPdtInStore()
                    .WithSerialNumber("PDT-SN-001"));

            var response = await _handler.HandleAsync(CreateRequest(), CancellationToken.None);

            var movement = _assetMovementRepository.AssetMovements.Single();

            Assert.AreEqual(movement.Id, response.Id);
            Assert.AreEqual(asset.Id, response.AssetId);
            Assert.AreEqual(asset.AssignedStoreId, response.StoreId);
            Assert.AreEqual("PDT-SN-001", response.SerialNumber);
            Assert.AreEqual(EmployeeNumber, response.EmployeeNumber);
            Assert.AreEqual(movement.CreatedAtUtc, response.CreatedAtUtc);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Asset_With_Rfid_Tag_Does_Not_Exist()
        {
            await Assert.ThrowsExactlyAsync<AssetNotFoundException>(
                () => _handler.HandleAsync(CreateRequest(), CancellationToken.None));

            Assert.AreEqual(RfidTagId, _assetRepository.LastRequestedRfidTagId);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumberCallCount);
            Assert.AreEqual(0, _assetMovementRepository.AddCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Employee_Does_Not_Exist()
        {
            var asset = SeedAsset(new AssetBuilder().AsPdtInStore());

            var request = new CreateAssetMovementRequest(RfidTagId, "999999");

            var exception = await Assert.ThrowsExactlyAsync<EmployeeNotFoundException>(
                () => _handler.HandleAsync(request, CancellationToken.None));

            Assert.AreEqual("999999", exception.EmployeeNumber);
            Assert.AreEqual(AssetStatus.In_Store, asset.AssetStatus);
            Assert.AreEqual(0, _assetMovementRepository.AddCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Asset_Is_Not_Pdt()
        {
            var asset = SeedAsset(
                new AssetBuilder()
                    .WithAssetType(AssetType.Notebook)
                    .WithAssetStatus(AssetStatus.In_Store));

            await Assert.ThrowsExactlyAsync<AssetMovementStateConflictException>(
                () => _handler.HandleAsync(CreateRequest(), CancellationToken.None));

            Assert.AreEqual(AssetStatus.In_Store, asset.AssetStatus);
            Assert.AreEqual(0, _assetMovementRepository.AddCallCount);
            Assert.AreEqual(0, _assetMovementRepository.SaveChangesCallCount);
        }

        [TestMethod]
        [DataRow(AssetStatus.In_HQ)]
        [DataRow(AssetStatus.In_Service)]
        [DataRow(AssetStatus.Route_To_Store)]
        [DataRow(AssetStatus.Disposed)]
        public async Task HandleAsync_Should_Throw_When_Pdt_Status_Does_Not_Allow_Movement(AssetStatus status)
        {
            var asset = SeedAsset(
                new AssetBuilder()
                    .WithAssetType(AssetType.PDT)
                    .WithAssetStatus(status));

            await Assert.ThrowsExactlyAsync<AssetMovementStateConflictException>(
                () => _handler.HandleAsync(CreateRequest(), CancellationToken.None));

            Assert.AreEqual(status, asset.AssetStatus);
            Assert.AreEqual(0, _assetMovementRepository.AddCallCount);
            Assert.AreEqual(0, _assetMovementRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Repositories()
        {
            SeedAsset(new AssetBuilder().AsPdtInStore());

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(CreateRequest(), cancellationToken);

            Assert.AreEqual(cancellationToken, _assetRepository.LastGetByRfidTagIdCancellationToken);
            Assert.AreEqual(cancellationToken, _employeeRepository.LastGetByEmployeeNumberCancellationToken);
            Assert.AreEqual(cancellationToken, _assetMovementRepository.LastAddCancellationToken);
            Assert.AreEqual(cancellationToken, _assetMovementRepository.LastSaveChangesCancellationToken);
        }

        private Asset SeedAsset(AssetBuilder builder)
        {
            var asset = builder
                .WithRfidTagId(RfidTagId)
                .Build();

            _assetRepository.Seed(asset);

            return asset;
        }

        private static CreateAssetMovementRequest CreateRequest()
        {
            return new CreateAssetMovementRequest(RfidTagId, EmployeeNumber);
        }
    }
}
