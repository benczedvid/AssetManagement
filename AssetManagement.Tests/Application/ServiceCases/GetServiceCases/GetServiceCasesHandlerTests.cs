using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Application.ServiceCases.GetServiceCases;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.ServiceCases.GetServiceCases
{
    [TestClass]
    public sealed class GetServiceCasesHandlerTests
    {
        private FakeServiceCaseRepository _serviceCaseRepository = null!;
        private FakeUserAccessScopeResolver _userAccessScopeResolver = null!;
        private FakeCurrentApplicationUserResolver _currentApplicationUserResolver = null!;
        private GetServiceCasesHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _serviceCaseRepository = new FakeServiceCaseRepository();
            _userAccessScopeResolver = new FakeUserAccessScopeResolver();
            _currentApplicationUserResolver = new FakeCurrentApplicationUserResolver();

            _currentApplicationUserResolver.SetCurrentUser(
                new ApplicationUserBuilder()
                    .AsApplicationAdministrator()
                    .Build());

            _handler = new GetServiceCasesHandler(
                _serviceCaseRepository,
                _userAccessScopeResolver,
                _currentApplicationUserResolver);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_ServiceCaseRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetServiceCasesHandler(null!, _userAccessScopeResolver, _currentApplicationUserResolver));

            Assert.AreEqual("serviceCaseRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_UserAccessScopeResolver_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetServiceCasesHandler(_serviceCaseRepository, null!, _currentApplicationUserResolver));

            Assert.AreEqual("userAccessScopeResolver", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_CurrentApplicationUserResolver_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetServiceCasesHandler(_serviceCaseRepository, _userAccessScopeResolver, null!));

            Assert.AreEqual("currentApplicationUserResolver", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_All_Service_Cases_For_Global_Scope()
        {
            _serviceCaseRepository.Seed(new ServiceCaseBuilder().WithCaseNumber("SC-2026-0001").AsOpenedAtHeadquarters().Build());
            _serviceCaseRepository.Seed(new ServiceCaseBuilder().WithCaseNumber("SC-2026-0002").AsOpenedFromStore(Guid.NewGuid()).Build());

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.HasCount(2, response.Items);
            Assert.AreEqual(1, _serviceCaseRepository.GetListCallCount);
            Assert.IsNull(_serviceCaseRepository.LastRequestedOriginStoreId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Map_Service_Case_Fields()
        {
            var originStoreId = Guid.NewGuid();
            var createdAtUtc = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

            var serviceCase = new ServiceCaseBuilder()
                .WithCaseNumber("SC-2026-0042")
                .WithIssueType(ServiceIssueType.DisplayIssue)
                .WithCreatedByUserName("Store User")
                .WithCreatedAtUtc(createdAtUtc)
                .AsOpenedFromStore(originStoreId)
                .Build();

            _serviceCaseRepository.Seed(serviceCase);

            var response = await _handler.HandleAsync(CancellationToken.None);

            var item = response.Items.Single();

            Assert.AreEqual(serviceCase.Id, item.Id);
            Assert.AreEqual("SC-2026-0042", item.CaseNumber);
            Assert.AreEqual(serviceCase.AssetId, item.AssetId);
            Assert.AreEqual(ServiceCaseStatus.AwaitingCentralReceipt, item.Status);
            Assert.AreEqual(ServiceIssueType.DisplayIssue, item.IssueType);
            Assert.AreEqual(originStoreId, item.OriginStoreId);
            Assert.IsNull(item.VendorId);
            Assert.IsTrue(item.IsActive);
            Assert.AreEqual(createdAtUtc, item.CreatedAtUtc);
            Assert.AreEqual("Store User", item.CreatedByUserName);
            Assert.IsNull(item.ClosedAtUtc);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Filter_By_Users_Store_For_Store_Scope()
        {
            var storeId = Guid.NewGuid();

            _currentApplicationUserResolver.SetCurrentUser(new ApplicationUserBuilder().AsStoreManagement(storeId).Build());
            _userAccessScopeResolver.SetStoreScope(storeId);

            var ownCase = new ServiceCaseBuilder().AsOpenedFromStore(storeId).Build();

            _serviceCaseRepository.Seed(ownCase);
            _serviceCaseRepository.Seed(new ServiceCaseBuilder().AsOpenedFromStore(Guid.NewGuid()).Build());

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.AreEqual(storeId, _serviceCaseRepository.LastRequestedOriginStoreId);
            Assert.AreEqual(ownCase.Id, response.Items.Single().Id);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Scope_Has_No_Store()
        {
            _userAccessScopeResolver.SetScope(new UserAccessScope(CanAccessAllStores: false, StoreId: null));

            await Assert.ThrowsExactlyAsync<ServiceCaseAccessDeniedException>(
                () => _handler.HandleAsync(CancellationToken.None));

            Assert.AreEqual(0, _serviceCaseRepository.GetListCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_User_Is_Inactive()
        {
            _currentApplicationUserResolver.SetCurrentUser(
                new ApplicationUserBuilder()
                    .AsApplicationAdministrator()
                    .AsInactive()
                    .Build());

            await Assert.ThrowsExactlyAsync<ServiceCaseAccessDeniedException>(
                () => _handler.HandleAsync(CancellationToken.None));

            Assert.AreEqual(0, _serviceCaseRepository.GetListCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Empty_List_When_No_Service_Cases_Exist()
        {
            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.IsNotNull(response.Items);
            Assert.IsEmpty(response.Items);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Dependencies()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(cancellationToken);

            Assert.AreEqual(cancellationToken, _currentApplicationUserResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _serviceCaseRepository.LastGetListCancellationToken);
        }
    }
}
