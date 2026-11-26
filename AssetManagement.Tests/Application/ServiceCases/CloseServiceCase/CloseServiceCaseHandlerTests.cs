using AssetManagement.Application.ServiceCases.CloseServiceCase;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.ServiceCases.CloseServiceCase
{
    [TestClass]
    public sealed class CloseServiceCaseHandlerTests
    {
        private FakeServiceCaseRepository _serviceCaseRepository = null!;
        private FakeServiceDeskRequestClosureOutboxRepository _closureOutboxRepository = null!;
        private FakeCurrentApplicationUserResolver _currentApplicationUserResolver = null!;
        private FakeAssetOperationExecutor _assetOperationExecutor = null!;
        private ApplicationUser _currentUser = null!;
        private CloseServiceCaseHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _serviceCaseRepository = new FakeServiceCaseRepository();
            _closureOutboxRepository = new FakeServiceDeskRequestClosureOutboxRepository();
            _currentApplicationUserResolver = new FakeCurrentApplicationUserResolver();
            _assetOperationExecutor = new FakeAssetOperationExecutor();

            _currentUser = new ApplicationUserBuilder()
                .AsApplicationAdministrator()
                .WithDisplayName("Admin User")
                .Build();

            _currentApplicationUserResolver.SetCurrentUser(_currentUser);

            _handler = new CloseServiceCaseHandler(
                _serviceCaseRepository,
                _closureOutboxRepository,
                _currentApplicationUserResolver,
                _assetOperationExecutor);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_ServiceCaseRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new CloseServiceCaseHandler(null!, _closureOutboxRepository, _currentApplicationUserResolver, _assetOperationExecutor));

            Assert.AreEqual("serviceCaseRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_ClosureOutboxRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new CloseServiceCaseHandler(_serviceCaseRepository, null!, _currentApplicationUserResolver, _assetOperationExecutor));

            Assert.AreEqual("closureOutboxRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_CurrentApplicationUserResolver_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new CloseServiceCaseHandler(_serviceCaseRepository, _closureOutboxRepository, null!, _assetOperationExecutor));

            Assert.AreEqual("currentApplicationUserResolver", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_AssetOperationExecutor_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new CloseServiceCaseHandler(_serviceCaseRepository, _closureOutboxRepository, _currentApplicationUserResolver, null!));

            Assert.AreEqual("assetOperationExecutor", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_ServiceCaseId_Is_Empty()
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => _handler.HandleAsync(Guid.Empty, new CloseServiceCaseRequest(null), CancellationToken.None));

            Assert.AreEqual("serviceCaseId", exception.ParamName);
            Assert.AreEqual(0, _serviceCaseRepository.GetByIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Request_Is_Null()
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => _handler.HandleAsync(Guid.NewGuid(), null!, CancellationToken.None));

            Assert.AreEqual("request", exception.ParamName);
            Assert.AreEqual(0, _serviceCaseRepository.GetByIdCallCount);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public async Task HandleAsync_Should_Throw_When_Comment_Is_Blank(string comment)
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => _handler.HandleAsync(Guid.NewGuid(), new CloseServiceCaseRequest(comment), CancellationToken.None));

            Assert.AreEqual("comment", exception.ParamName);
            Assert.AreEqual(0, _serviceCaseRepository.GetByIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Comment_Is_Too_Long()
        {
            var request = new CloseServiceCaseRequest(new string('a', 2001));

            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => _handler.HandleAsync(Guid.NewGuid(), request, CancellationToken.None));

            Assert.AreEqual("comment", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Service_Case_Does_Not_Exist()
        {
            var serviceCaseId = Guid.NewGuid();

            var exception = await Assert.ThrowsExactlyAsync<ServiceCaseNotFoundException>(
                () => _handler.HandleAsync(serviceCaseId, new CloseServiceCaseRequest(null), CancellationToken.None));

            Assert.AreEqual(serviceCaseId, exception.ServiceCaseId);
            Assert.AreEqual(0, _assetOperationExecutor.ExecuteCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Lock_The_Service_Case_Asset()
        {
            var serviceCase = SeedResolvedServiceCase();

            await _handler.HandleAsync(serviceCase.Id, new CloseServiceCaseRequest(null), CancellationToken.None);

            Assert.AreEqual(1, _assetOperationExecutor.ExecuteCallCount);
            CollectionAssert.AreEqual(new[] { serviceCase.AssetId }, _assetOperationExecutor.LastLockedAssetIds.ToArray());
        }

        [TestMethod]
        public async Task HandleAsync_Should_Close_Resolved_Service_Case()
        {
            var serviceCase = SeedResolvedServiceCase();

            var response = await _handler.HandleAsync(serviceCase.Id, new CloseServiceCaseRequest(null), CancellationToken.None);

            Assert.AreEqual(ServiceCaseStatus.Closed, serviceCase.Status);
            Assert.AreEqual(serviceCase.Id, response.ServiceCaseId);
            Assert.AreEqual(serviceCase.CaseNumber, response.CaseNumber);
            Assert.AreEqual(serviceCase.AssetId, response.AssetId);
            Assert.AreEqual(ServiceCaseStatus.Closed, response.ServiceCaseStatus);
            Assert.AreEqual(_currentUser.Id, response.ClosedByUserId);
            Assert.AreEqual("Admin User", response.ClosedByUserName);
            Assert.AreEqual(1, _serviceCaseRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Record_Closed_Event()
        {
            var serviceCase = SeedResolvedServiceCase();

            await _handler.HandleAsync(serviceCase.Id, new CloseServiceCaseRequest("Repair confirmed."), CancellationToken.None);

            var serviceCaseEvent = _serviceCaseRepository.Events.Single();

            Assert.AreEqual(serviceCase.Id, serviceCaseEvent.ServiceCaseId);
            Assert.AreEqual(ServiceCaseEventType.Closed, serviceCaseEvent.Type);
            Assert.AreEqual(ServiceCaseStatus.Resolved, serviceCaseEvent.PreviousStatus);
            Assert.AreEqual(ServiceCaseStatus.Closed, serviceCaseEvent.NewStatus);
            Assert.AreEqual("Repair confirmed.", serviceCaseEvent.Comment);
            Assert.AreEqual(_currentUser.Id, serviceCaseEvent.PerformedByUserId);
            Assert.AreEqual("Admin User", serviceCaseEvent.PerformedByUserName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Register_ServiceDesk_Request_Closure()
        {
            var serviceCase = SeedResolvedServiceCase();

            await _handler.HandleAsync(serviceCase.Id, new CloseServiceCaseRequest(null), CancellationToken.None);

            var outbox = _closureOutboxRepository.Outboxes.Single();
            var serviceCaseEvent = _serviceCaseRepository.Events.Single();

            Assert.AreEqual(serviceCase.Id, outbox.ServiceCaseId);
            Assert.AreEqual(serviceCaseEvent.Id, outbox.ServiceCaseEventId);
            Assert.AreEqual(ServiceDeskRequestClosureOutboxStatus.Pending, outbox.Status);
        }

        [TestMethod]
        [DataRow(ServiceCaseStatus.UnderCentralInspection)]
        [DataRow(ServiceCaseStatus.AwaitingCentralReceipt)]
        public async Task HandleAsync_Should_Throw_When_Service_Case_Is_Not_Resolved(ServiceCaseStatus status)
        {
            var serviceCase = new ServiceCaseBuilder()
                .WithInitialStatus(status)
                .WithOriginStoreId(status == ServiceCaseStatus.AwaitingCentralReceipt ? Guid.NewGuid() : null)
                .Build();

            _serviceCaseRepository.Seed(serviceCase);

            await Assert.ThrowsExactlyAsync<ServiceCaseStateConflictException>(
                () => _handler.HandleAsync(serviceCase.Id, new CloseServiceCaseRequest(null), CancellationToken.None));

            Assert.AreEqual(status, serviceCase.Status);
            AssertNothingWasPersisted();
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Asset_Changed_Before_Lock_Was_Acquired()
        {
            var serviceCase = SeedResolvedServiceCase();

            _serviceCaseRepository.ServiceCaseReturnedForUpdate = CreateResolvedServiceCase(Guid.NewGuid());

            await Assert.ThrowsExactlyAsync<ServiceCaseStateConflictException>(
                () => _handler.HandleAsync(serviceCase.Id, new CloseServiceCaseRequest(null), CancellationToken.None));

            AssertNothingWasPersisted();
        }

        [TestMethod]
        [DataRow(ApplicationRole.HotlineAgent)]
        [DataRow(ApplicationRole.CentralUser)]
        public async Task HandleAsync_Should_Throw_When_User_Cannot_Close_Service_Cases(ApplicationRole role)
        {
            var serviceCase = SeedResolvedServiceCase();

            _currentApplicationUserResolver.SetCurrentUser(new ApplicationUserBuilder().WithRole(role).Build());

            await Assert.ThrowsExactlyAsync<ServiceCaseAccessDeniedException>(
                () => _handler.HandleAsync(serviceCase.Id, new CloseServiceCaseRequest(null), CancellationToken.None));

            Assert.AreEqual(ServiceCaseStatus.Resolved, serviceCase.Status);
            AssertNothingWasPersisted();
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_User_Is_Inactive()
        {
            var serviceCase = SeedResolvedServiceCase();

            _currentApplicationUserResolver.SetCurrentUser(
                new ApplicationUserBuilder()
                    .AsApplicationAdministrator()
                    .AsInactive()
                    .Build());

            await Assert.ThrowsExactlyAsync<ServiceCaseAccessDeniedException>(
                () => _handler.HandleAsync(serviceCase.Id, new CloseServiceCaseRequest(null), CancellationToken.None));

            AssertNothingWasPersisted();
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Dependencies()
        {
            var serviceCase = SeedResolvedServiceCase();

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(serviceCase.Id, new CloseServiceCaseRequest(null), cancellationToken);

            Assert.AreEqual(cancellationToken, _serviceCaseRepository.LastGetByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _assetOperationExecutor.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _currentApplicationUserResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _serviceCaseRepository.LastGetForUpdateByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _serviceCaseRepository.LastAddEventCancellationToken);
            Assert.AreEqual(cancellationToken, _closureOutboxRepository.LastAddCancellationToken);
            Assert.AreEqual(cancellationToken, _serviceCaseRepository.LastSaveChangesCancellationToken);
        }

        private ServiceCase SeedResolvedServiceCase()
        {
            var serviceCase = CreateResolvedServiceCase(Guid.NewGuid());

            _serviceCaseRepository.Seed(serviceCase);

            return serviceCase;
        }

        private static ServiceCase CreateResolvedServiceCase(Guid assetId)
        {
            var serviceCase = new ServiceCaseBuilder()
                .WithAssetId(assetId)
                .AsOpenedAtHeadquarters()
                .Build();

            serviceCase.CompleteRepairAtHeadquarters();

            return serviceCase;
        }

        private void AssertNothingWasPersisted()
        {
            Assert.AreEqual(0, _serviceCaseRepository.AddEventCallCount);
            Assert.AreEqual(0, _closureOutboxRepository.AddCallCount);
            Assert.AreEqual(0, _serviceCaseRepository.SaveChangesCallCount);
        }
    }
}
