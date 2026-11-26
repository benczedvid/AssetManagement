using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Employees.GetEmployees;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Employees.GetEmployees
{
    [TestClass]
    public sealed class GetEmployeesHandlerTests
    {
        private FakeEmployeeRepository _employeeRepository = null!;
        private FakeUserAccessScopeResolver _userAccessScopeResolver = null!;
        private GetEmployeesHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _employeeRepository = new FakeEmployeeRepository();
            _userAccessScopeResolver = new FakeUserAccessScopeResolver();
            _handler = new GetEmployeesHandler(_employeeRepository, _userAccessScopeResolver);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_EmployeeRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetEmployeesHandler(null!, _userAccessScopeResolver));

            Assert.AreEqual("employeeRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_UserAccessScopeResolver_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetEmployeesHandler(_employeeRepository, null!));

            Assert.AreEqual("userAccessScopeResolver", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_All_Employees_For_Global_Scope()
        {
            _userAccessScopeResolver.SetGlobalScope();

            _employeeRepository.Seed(
            [
                new EmployeeBuilder().WithEmployeeNumber("100001").WithStoreNumber("321").Build(),
                new EmployeeBuilder().WithEmployeeNumber("100002").WithStoreNumber("322").Build()
            ]);

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.HasCount(2, response);
            Assert.AreEqual(1, _employeeRepository.GetAllCallCount);
            Assert.AreEqual(0, _employeeRepository.GetAllByStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Query_Employees_By_Store_For_Store_Scope()
        {
            var storeId = Guid.NewGuid();

            _userAccessScopeResolver.SetStoreScope(storeId);

            await _handler.HandleAsync(CancellationToken.None);

            Assert.AreEqual(0, _employeeRepository.GetAllCallCount);
            Assert.AreEqual(1, _employeeRepository.GetAllByStoreIdCallCount);
            Assert.AreEqual(storeId, _employeeRepository.LastRequestedStoreId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Scope_Has_No_Store()
        {
            _userAccessScopeResolver.SetScope(new UserAccessScope(CanAccessAllStores: false, StoreId: null));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _handler.HandleAsync(CancellationToken.None));

            Assert.AreEqual(0, _employeeRepository.GetAllCallCount);
            Assert.AreEqual(0, _employeeRepository.GetAllByStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Map_Employee_Fields()
        {
            var employee = new EmployeeBuilder()
                .WithEmployeeNumber("103549")
                .WithFullName("John Doe")
                .WithEmploymentStartDate(new DateTime(2020, 1, 1))
                .WithEmploymentEndDate(new DateTime(2026, 12, 31))
                .WithStoreNumber("321")
                .WithOrganizationType("Store")
                .WithPositionCode("STORE_USER")
                .WithPosition("Store employee")
                .WithPhoneNumber("+36 30 123 4567")
                .WithEmail("john.doe@example.com")
                .Build();

            _employeeRepository.Seed(employee);

            var response = await _handler.HandleAsync(CancellationToken.None);

            var result = response.Single();

            Assert.AreEqual(employee.EmployeeNumber, result.EmployeeNumber);
            Assert.AreEqual(employee.FullName, result.FullName);
            Assert.AreEqual(employee.EmploymentStartDate, result.EmploymentStartDate);
            Assert.AreEqual(employee.EmploymentEndDate, result.EmploymentEndDate);
            Assert.AreEqual(employee.StoreNumber, result.StoreNumber);
            Assert.AreEqual(employee.OrganizationType, result.OrganizationType);
            Assert.AreEqual(employee.PositionCode, result.PositionCode);
            Assert.AreEqual(employee.Position, result.Position);
            Assert.AreEqual(employee.PhoneNumber, result.PhoneNumber);
            Assert.AreEqual(employee.Email, result.Email);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Empty_List_When_No_Employees_Exist()
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
            Assert.AreEqual(cancellationToken, _employeeRepository.LastGetAllCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Store_Query()
        {
            _userAccessScopeResolver.SetStoreScope(Guid.NewGuid());

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(cancellationToken);

            Assert.AreEqual(cancellationToken, _employeeRepository.LastGetAllByStoreIdCancellationToken);
        }
    }
}
