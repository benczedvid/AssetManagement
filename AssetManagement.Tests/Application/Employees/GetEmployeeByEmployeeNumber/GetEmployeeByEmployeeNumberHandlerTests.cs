using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Employees.GetEmployeeByEmployeeNumber;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Employees.GetEmployeeByEmployeeNumber
{
    [TestClass]
    public sealed class GetEmployeeByEmployeeNumberHandlerTests
    {
        private FakeEmployeeRepository _employeeRepository = null!;
        private FakeUserAccessScopeResolver _userAccessScopeResolver = null!;
        private GetEmployeeByEmployeeNumberHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _employeeRepository = new FakeEmployeeRepository();
            _userAccessScopeResolver = new FakeUserAccessScopeResolver();
            _handler = new GetEmployeeByEmployeeNumberHandler(_employeeRepository, _userAccessScopeResolver);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_EmployeeRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetEmployeeByEmployeeNumberHandler(null!, _userAccessScopeResolver));

            Assert.AreEqual("employeeRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_UserAccessScopeResolver_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetEmployeeByEmployeeNumberHandler(_employeeRepository, null!));

            Assert.AreEqual("userAccessScopeResolver", exception.ParamName);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task HandleAsync_Should_Throw_When_EmployeeNumber_Is_Invalid(string? employeeNumber)
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.HandleAsync(employeeNumber!, CancellationToken.None));

            Assert.AreEqual("employeeNumber", exception.ParamName);
            Assert.AreEqual(0, _userAccessScopeResolver.ResolveCallCount);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumberCallCount);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumberAndStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Employee_For_Global_Scope()
        {
            var employee = new EmployeeBuilder()
                .WithEmployeeNumber("103549")
                .WithFullName("John Doe")
                .Build();

            _employeeRepository.Seed(employee);

            var response = await _handler.HandleAsync("103549", CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(employee.EmployeeNumber, response.EmployeeNumber);
            Assert.AreEqual(employee.FullName, response.FullName);
            Assert.AreEqual(employee.EmploymentStartDate, response.EmploymentStartDate);
            Assert.AreEqual(employee.EmploymentEndDate, response.EmploymentEndDate);
            Assert.AreEqual(employee.StoreNumber, response.StoreNumber);
            Assert.AreEqual(employee.OrganizationType, response.OrganizationType);
            Assert.AreEqual(employee.PositionCode, response.PositionCode);
            Assert.AreEqual(employee.Position, response.Position);
            Assert.AreEqual(employee.PhoneNumber, response.PhoneNumber);
            Assert.AreEqual(employee.Email, response.Email);
            Assert.AreEqual(1, _employeeRepository.GetByEmployeeNumberCallCount);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumberAndStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Trim_EmployeeNumber()
        {
            _employeeRepository.Seed(new EmployeeBuilder().WithEmployeeNumber("103549").Build());

            var response = await _handler.HandleAsync("  103549  ", CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual("103549", _employeeRepository.LastRequestedEmployeeNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Null_When_Employee_Does_Not_Exist()
        {
            var response = await _handler.HandleAsync("999999", CancellationToken.None);

            Assert.IsNull(response);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Query_By_Store_For_Store_Scope()
        {
            var storeId = Guid.NewGuid();

            _userAccessScopeResolver.SetStoreScope(storeId);

            await _handler.HandleAsync("103549", CancellationToken.None);

            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumberCallCount);
            Assert.AreEqual(1, _employeeRepository.GetByEmployeeNumberAndStoreIdCallCount);
            Assert.AreEqual("103549", _employeeRepository.LastRequestedEmployeeNumber);
            Assert.AreEqual(storeId, _employeeRepository.LastRequestedStoreId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Null_When_Employee_Is_Not_In_Users_Store()
        {
            _employeeRepository.Seed(
                new EmployeeBuilder()
                    .WithEmployeeNumber("103549")
                    .WithStoreNumber("321")
                    .Build());

            _userAccessScopeResolver.SetStoreScope(Guid.NewGuid());

            var response = await _handler.HandleAsync("103549", CancellationToken.None);

            Assert.IsNull(response);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Scope_Has_No_Store()
        {
            _userAccessScopeResolver.SetScope(new UserAccessScope(CanAccessAllStores: false, StoreId: null));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _handler.HandleAsync("103549", CancellationToken.None));

            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumberCallCount);
            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumberAndStoreIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Dependencies()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync("103549", cancellationToken);

            Assert.AreEqual(cancellationToken, _userAccessScopeResolver.LastCancellationToken);
            Assert.AreEqual(cancellationToken, _employeeRepository.LastGetByEmployeeNumberCancellationToken);
        }
    }
}
