using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Users.GetOrCreateCurrentUser;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Users
{
    [TestClass]
    public sealed class GetOrCreateCurrentUserHandlerTests
    {
        private FakeCurrentUser _currentUser = null!;
        private FakeApplicationUserRepository _userRepository = null!;
        private FakeEntraUserProfileService _entraUserProfileService = null!;
        private FakeEmployeeRepository _employeeRepository = null!;
        private FakeStoreRepository _storeRepository = null!;
        private GetOrCreateCurrentUserHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _currentUser = new FakeCurrentUser
            {
                IsAuthenticated = true,
                EntraObjectId = Guid.NewGuid(),
                EntraTenantId = Guid.NewGuid(),
                Roles =
                [
                    ApplicationRole.ApplicationAdministrator
                ]
            };

            _userRepository = new FakeApplicationUserRepository();

            _entraUserProfileService = new FakeEntraUserProfileService();

            _employeeRepository = new FakeEmployeeRepository();

            _storeRepository = new FakeStoreRepository();

            _handler = CreateHandler();
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_CurrentUser_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetOrCreateCurrentUserHandler(
                        currentUser: null!,
                        userRepository: _userRepository,
                        entraUserProfileService: _entraUserProfileService,
                        employeeRepository: _employeeRepository,
                        storeRepository: _storeRepository));

            Assert.AreEqual("currentUser", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_UserRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetOrCreateCurrentUserHandler(
                        currentUser: _currentUser,
                        userRepository: null!,
                        entraUserProfileService:
                            _entraUserProfileService,
                        employeeRepository:
                            _employeeRepository,
                        storeRepository:
                            _storeRepository));

            Assert.AreEqual("userRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_EntraUserProfileService_Is_Null()
        {
            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetOrCreateCurrentUserHandler(
                        currentUser: _currentUser,
                        userRepository: _userRepository,
                        entraUserProfileService: null!,
                        employeeRepository:
                            _employeeRepository,
                        storeRepository:
                            _storeRepository));

            Assert.AreEqual("entraUserProfileService", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_EmployeeRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetOrCreateCurrentUserHandler(
                        currentUser: _currentUser,
                        userRepository: _userRepository,
                        entraUserProfileService:
                            _entraUserProfileService,
                        employeeRepository: null!,
                        storeRepository:
                            _storeRepository));

            Assert.AreEqual("employeeRepository", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_StoreRepository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                    () => new GetOrCreateCurrentUserHandler(
                        currentUser: _currentUser,
                        userRepository: _userRepository,
                        entraUserProfileService:
                            _entraUserProfileService,
                        employeeRepository:
                            _employeeRepository,
                        storeRepository: null!));

            Assert.AreEqual("storeRepository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_User_Is_Not_Authenticated()
        {
            _currentUser.IsAuthenticated = false;

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _entraUserProfileService.GetByObjectIdCallCount);
            Assert.AreEqual(0, _userRepository.GetByEntraIdentityCallCount);
            Assert.AreEqual(0, _userRepository.AddCallCount);
            Assert.AreEqual(0, _userRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_EntraObjectId_Is_Empty()
        {
            _currentUser.EntraObjectId = Guid.Empty;

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _entraUserProfileService.GetByObjectIdCallCount);
            Assert.AreEqual(0, _userRepository.GetByEntraIdentityCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_EntraTenantId_Is_Empty()
        {
            _currentUser.EntraTenantId = Guid.Empty;

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _entraUserProfileService.GetByObjectIdCallCount);
            Assert.AreEqual(0, _userRepository.GetByEntraIdentityCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Roles_Are_Null()
        {
            _currentUser.Roles = null!;

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _entraUserProfileService.GetByObjectIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_User_Has_No_Role()
        {
            _currentUser.Roles = [];

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _entraUserProfileService.GetByObjectIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_User_Has_Multiple_Roles()
        {
            _currentUser.Roles =
            [
                ApplicationRole.ApplicationAdministrator,
                ApplicationRole.CentralUser
            ];

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _entraUserProfileService.GetByObjectIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Role_Is_Unknown()
        {
            _currentUser.Roles =
            [
                ApplicationRole.Unknown
            ];

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _entraUserProfileService.GetByObjectIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Role_Is_Not_Defined()
        {
            _currentUser.Roles =
            [
                (ApplicationRole)int.MaxValue
            ];

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _entraUserProfileService.GetByObjectIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Entra_Profile_Does_Not_Exist()
        {
            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(1, _entraUserProfileService.GetByObjectIdCallCount);
            Assert.AreEqual(_currentUser.EntraObjectId, _entraUserProfileService.LastRequestedEntraObjectId);
            Assert.AreEqual(0, _userRepository.GetByEntraIdentityCallCount);
            Assert.AreEqual(0, _userRepository.AddCallCount);
            Assert.AreEqual(0, _userRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Profile_Belongs_To_Different_User()
        {
            SeedProfile(CreateValidProfile(entraObjectId: Guid.NewGuid()));

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _userRepository.GetByEntraIdentityCallCount);
            Assert.AreEqual(0, _userRepository.AddCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Profile_DisplayName_Is_Empty()
        {
            SeedProfile(CreateValidProfile(displayName: string.Empty));

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _userRepository.GetByEntraIdentityCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Profile_DisplayName_Is_Whitespace()
        {
            SeedProfile(CreateValidProfile(displayName: " "));

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => _handler.HandleAsync());

            Assert.AreEqual(0, _userRepository.GetByEntraIdentityCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Create_ApplicationAdministrator_When_User_Does_Not_Exist()
        {
            var profile = CreateValidProfile();

            SeedProfile(profile);

            var result = await _handler.HandleAsync();

            Assert.AreNotEqual(Guid.Empty, result.Id);
            Assert.AreEqual(_currentUser.EntraObjectId, result.EntraObjectId);
            Assert.AreEqual(_currentUser.EntraTenantId, result.EntraTenantId);
            Assert.AreEqual(profile.EmployeeNumber, result.EmployeeNumber);
            Assert.AreEqual(profile.FirstName, result.FirstName);
            Assert.AreEqual(profile.LastName, result.LastName);
            Assert.AreEqual(profile.DisplayName, result.DisplayName);
            Assert.AreEqual(ApplicationRole.ApplicationAdministrator, result.Role);

            Assert.IsNull(result.StoreId);
            Assert.IsTrue(result.CanAccessAllStores);
            Assert.IsTrue(result.IsActive);

            Assert.AreEqual(1,_userRepository.GetByEntraIdentityCallCount);
            Assert.AreEqual(1, _userRepository.AddCallCount);
            Assert.AreEqual(1, _userRepository.SaveChangesCallCount);
            Assert.HasCount(1, _userRepository.Users);

            var createdUser = _userRepository.Users.Single();

            Assert.AreEqual(result.Id, createdUser.Id);
            Assert.AreEqual(profile.FirstName, createdUser.FirstName);
            Assert.AreEqual(profile.LastName, createdUser.LastName);
            Assert.AreEqual(profile.DisplayName, createdUser.DisplayName);
            Assert.AreEqual(profile.Mail, createdUser.Mail);
            Assert.AreEqual(profile.Department, createdUser.Department);
            Assert.AreEqual(profile.JobTitle, createdUser.JobTitle);
            Assert.AreEqual(profile.MobilePhone, createdUser.MobilePhone);
            Assert.AreEqual(ApplicationRole.ApplicationAdministrator, createdUser.Role);

            Assert.IsNull(createdUser.StoreId);
            Assert.IsTrue(createdUser.IsActive);
            Assert.IsNotNull(createdUser.LastLoginAtUtc);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Create_CentralUser_With_Global_Access()
        {
            _currentUser.Roles =
            [
                ApplicationRole.CentralUser
            ];

            SeedProfile(CreateValidProfile());

            var result = await _handler.HandleAsync();

            Assert.AreEqual(ApplicationRole.CentralUser, result.Role);

            Assert.IsTrue(result.CanAccessAllStores);
            Assert.IsNull(result.StoreId);
            Assert.IsTrue(result.IsActive);

            Assert.AreEqual(0, _employeeRepository.GetByEmployeeNumberCallCount);
            Assert.AreEqual(0, _storeRepository.GetByStoreNumberCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_And_Synchronize_Existing_User()
        {
            var existingUser = new ApplicationUserBuilder()
                .WithEntraObjectId(_currentUser.EntraObjectId)
                .WithEntraTenantId(_currentUser.EntraTenantId)
                .WithFirstName("Old")
                .WithLastName("User")
                .WithDisplayName("Old User")
                .WithMail("old@example.com")
                .WithDepartment("Old department")
                .WithJobTitle("Old job title")
                .WithMobilePhone("000")
                .AsApplicationAdministrator()
                .Build();

            _userRepository.Seed(existingUser);

            var profile = CreateValidProfile(
                employeeNumber: "103549",
                firstName: "John",
                lastName: "Doe",
                displayName: "John Doe",
                mail: "johndoe@example.com",
                department: "Accounting",
                jobTitle: "Accountant",
                mobilePhone: "+36 30 111 2222");

            SeedProfile(profile);

            var result = await _handler.HandleAsync();

            Assert.AreEqual(existingUser.Id, result.Id);
            Assert.AreEqual("John", existingUser.FirstName);
            Assert.AreEqual("Doe", existingUser.LastName);
            Assert.AreEqual("John Doe", existingUser.DisplayName);
            Assert.AreEqual("johndoe@example.com", existingUser.Mail);
            Assert.AreEqual("Accounting", existingUser.Department);
            Assert.AreEqual("Accounting", existingUser.JobTitle);
            Assert.AreEqual("+36 30 111 2222", existingUser.MobilePhone);
            Assert.AreEqual(ApplicationRole.ApplicationAdministrator, existingUser.Role);
            Assert.IsNull(existingUser.StoreId);
            Assert.IsTrue(existingUser.IsActive);
            Assert.IsNotNull(existingUser.LastLoginAtUtc);
            Assert.AreEqual(0, _userRepository.AddCallCount);
            Assert.AreEqual(1, _userRepository.SaveChangesCallCount);
            Assert.HasCount(1, _userRepository.Users);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Create_StoreAdministrator_With_Resolved_Store()
        {
            const string employeeNumber = "103549";
            const string storeNumber = "321";

            _currentUser.Roles =
            [
                ApplicationRole.StoreAdministrator
            ];

            var profile = CreateValidProfile(employeeNumber: employeeNumber);

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithStoreNumber(storeNumber)
                .Build();

            var store = new StoreBuilder()
                .WithStoreNumber(storeNumber)
                .Build();

            SeedProfile(profile);
            _employeeRepository.Seed(employee);
            _storeRepository.Seed(store);

            var result = await _handler.HandleAsync();

            Assert.AreEqual(
                ApplicationRole.StoreAdministrator,
                result.Role);

            Assert.AreEqual(
                store.Id,
                result.StoreId);

            Assert.AreEqual(
                employeeNumber,
                result.EmployeeNumber);

            Assert.IsFalse(result.CanAccessAllStores);
            Assert.IsTrue(result.IsActive);

            Assert.AreEqual(
                1,
                _employeeRepository.GetByEmployeeNumberCallCount);

            Assert.AreEqual(
                employeeNumber,
                _employeeRepository.LastRequestedEmployeeNumber);

            Assert.AreEqual(
                1,
                _storeRepository.GetByStoreNumberCallCount);

            var createdUser =
                _userRepository.Users.Single();

            Assert.AreEqual(
                store.Id,
                createdUser.StoreId);

            Assert.AreEqual(
                ApplicationRole.StoreAdministrator,
                createdUser.Role);

            Assert.IsTrue(createdUser.IsActive);
            Assert.IsNotNull(createdUser.LastLoginAtUtc);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Create_StoreManagement_User_With_Resolved_Store()
        {
            const string employeeNumber = "103549";
            const string storeNumber = "321";

            _currentUser.Roles =
            [
                ApplicationRole.StoreManagement
            ];

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithStoreNumber(storeNumber)
                .Build();

            var store = new StoreBuilder()
                .WithStoreNumber(storeNumber)
                .Build();

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: employeeNumber));

            _employeeRepository.Seed(employee);
            _storeRepository.Seed(store);

            var result = await _handler.HandleAsync();

            Assert.AreEqual(
                ApplicationRole.StoreManagement,
                result.Role);

            Assert.AreEqual(store.Id, result.StoreId);
            Assert.IsFalse(result.CanAccessAllStores);
            Assert.IsTrue(result.IsActive);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Change_Existing_Global_User_To_Store_User()
        {
            const string employeeNumber = "103549";
            const string storeNumber = "321";

            var existingUser = new ApplicationUserBuilder()
                .WithEntraObjectId(
                    _currentUser.EntraObjectId)
                .WithEntraTenantId(
                    _currentUser.EntraTenantId)
                .AsApplicationAdministrator()
                .Build();

            _currentUser.Roles =
            [
                ApplicationRole.StoreAdministrator
            ];

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithStoreNumber(storeNumber)
                .Build();

            var store = new StoreBuilder()
                .WithStoreNumber(storeNumber)
                .Build();

            _userRepository.Seed(existingUser);

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: employeeNumber));

            _employeeRepository.Seed(employee);
            _storeRepository.Seed(store);

            var result = await _handler.HandleAsync();

            Assert.AreEqual(
                existingUser.Id,
                result.Id);

            Assert.AreEqual(
                ApplicationRole.StoreAdministrator,
                existingUser.Role);

            Assert.AreEqual(
                store.Id,
                existingUser.StoreId);

            Assert.IsFalse(result.CanAccessAllStores);

            Assert.AreEqual(
                0,
                _userRepository.AddCallCount);

            Assert.AreEqual(
                1,
                _userRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Change_Existing_Store_User_To_Global_User()
        {
            var existingStoreId = Guid.NewGuid();

            var existingUser = new ApplicationUserBuilder()
                .WithEntraObjectId(
                    _currentUser.EntraObjectId)
                .WithEntraTenantId(
                    _currentUser.EntraTenantId)
                .AsStoreAdministrator(existingStoreId)
                .Build();

            _currentUser.Roles =
            [
                ApplicationRole.CentralUser
            ];

            _userRepository.Seed(existingUser);
            SeedProfile(CreateValidProfile());

            var result = await _handler.HandleAsync();

            Assert.AreEqual(
                ApplicationRole.CentralUser,
                existingUser.Role);

            Assert.IsNull(existingUser.StoreId);
            Assert.IsTrue(result.CanAccessAllStores);
            Assert.IsNull(result.StoreId);

            Assert.AreEqual(
                0,
                _employeeRepository.GetByEmployeeNumberCallCount);

            Assert.AreEqual(
                0,
                _storeRepository.GetByStoreNumberCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_User_Has_No_EmployeeNumber()
        {
            _currentUser.Roles =
            [
                ApplicationRole.StoreAdministrator
            ];

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: null));

            await Assert.ThrowsExactlyAsync<
                UnauthorizedAccessException>(
                () => _handler.HandleAsync());

            Assert.AreEqual(
                0,
                _employeeRepository.GetByEmployeeNumberCallCount);

            Assert.AreEqual(
                0,
                _storeRepository.GetByStoreNumberCallCount);

            Assert.AreEqual(
                0,
                _userRepository.GetByEntraIdentityCallCount);

            Assert.AreEqual(
                0,
                _userRepository.AddCallCount);

            Assert.AreEqual(
                0,
                _userRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_User_EmployeeNumber_Is_Whitespace()
        {
            _currentUser.Roles =
            [
                ApplicationRole.StoreManagement
            ];

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: " "));

            await Assert.ThrowsExactlyAsync<
                UnauthorizedAccessException>(
                () => _handler.HandleAsync());

            Assert.AreEqual(
                0,
                _employeeRepository.GetByEmployeeNumberCallCount);

            Assert.AreEqual(
                0,
                _storeRepository.GetByStoreNumberCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Trim_EmployeeNumber_Before_Employee_Lookup()
        {
            const string employeeNumber = "103549";
            const string storeNumber = "321";

            _currentUser.Roles =
            [
                ApplicationRole.StoreAdministrator
            ];

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithStoreNumber(storeNumber)
                .Build();

            var store = new StoreBuilder()
                .WithStoreNumber(storeNumber)
                .Build();

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: "  103549  "));

            _employeeRepository.Seed(employee);
            _storeRepository.Seed(store);

            var result = await _handler.HandleAsync();

            Assert.AreEqual(
                employeeNumber,
                _employeeRepository.LastRequestedEmployeeNumber);

            Assert.AreEqual(
                employeeNumber,
                result.EmployeeNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Employee_Does_Not_Exist()
        {
            _currentUser.Roles =
            [
                ApplicationRole.StoreAdministrator
            ];

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: "999999"));

            await Assert.ThrowsExactlyAsync<
                UnauthorizedAccessException>(
                () => _handler.HandleAsync());

            Assert.AreEqual(
                1,
                _employeeRepository.GetByEmployeeNumberCallCount);

            Assert.AreEqual(
                0,
                _storeRepository.GetByStoreNumberCallCount);

            Assert.AreEqual(
                0,
                _userRepository.GetByEntraIdentityCallCount);

            Assert.AreEqual(
                0,
                _userRepository.AddCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Employee_Has_No_StoreNumber()
        {
            const string employeeNumber = "103549";

            _currentUser.Roles =
            [
                ApplicationRole.StoreManagement
            ];

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithStoreNumber(null)
                .Build();

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: employeeNumber));

            _employeeRepository.Seed(employee);

            await Assert.ThrowsExactlyAsync<
                UnauthorizedAccessException>(
                () => _handler.HandleAsync());

            Assert.AreEqual(
                1,
                _employeeRepository.GetByEmployeeNumberCallCount);

            Assert.AreEqual(
                0,
                _storeRepository.GetByStoreNumberCallCount);

            Assert.AreEqual(
                0,
                _userRepository.GetByEntraIdentityCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Employee_StoreNumber_Is_Whitespace()
        {
            const string employeeNumber = "103549";

            _currentUser.Roles =
            [
                ApplicationRole.StoreManagement
            ];

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithStoreNumber(" ")
                .Build();

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: employeeNumber));

            _employeeRepository.Seed(employee);

            await Assert.ThrowsExactlyAsync<
                UnauthorizedAccessException>(
                () => _handler.HandleAsync());

            Assert.AreEqual(
                0,
                _storeRepository.GetByStoreNumberCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Trim_StoreNumber_Before_Store_Lookup()
        {
            const string employeeNumber = "103549";
            const string storeNumber = "321";

            _currentUser.Roles =
            [
                ApplicationRole.StoreAdministrator
            ];

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithStoreNumber("  321  ")
                .Build();

            var store = new StoreBuilder()
                .WithStoreNumber(storeNumber)
                .Build();

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: employeeNumber));

            _employeeRepository.Seed(employee);
            _storeRepository.Seed(store);

            var result = await _handler.HandleAsync();

            Assert.AreEqual(
                store.Id,
                result.StoreId);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Employee_Store_Does_Not_Exist()
        {
            const string employeeNumber = "103549";

            _currentUser.Roles =
            [
                ApplicationRole.StoreAdministrator
            ];

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithStoreNumber("999")
                .Build();

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: employeeNumber));

            _employeeRepository.Seed(employee);

            await Assert.ThrowsExactlyAsync<
                UnauthorizedAccessException>(
                () => _handler.HandleAsync());

            Assert.AreEqual(
                1,
                _storeRepository.GetByStoreNumberCallCount);

            Assert.AreEqual(
                0,
                _userRepository.GetByEntraIdentityCallCount);

            Assert.AreEqual(
                0,
                _userRepository.AddCallCount);

            Assert.AreEqual(
                0,
                _userRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Create_Save_And_Reject_Disabled_New_User()
        {
            SeedProfile(
                CreateValidProfile(
                    isAccountEnabled: false));

            await Assert.ThrowsExactlyAsync<
                UnauthorizedAccessException>(
                () => _handler.HandleAsync());

            Assert.AreEqual(
                1,
                _userRepository.AddCallCount);

            Assert.AreEqual(
                1,
                _userRepository.SaveChangesCallCount);

            Assert.HasCount(
                1,
                _userRepository.Users);

            var createdUser =
                _userRepository.Users.Single();

            Assert.IsFalse(createdUser.IsActive);
            Assert.IsNull(createdUser.LastLoginAtUtc);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Synchronize_Save_And_Reject_Disabled_Existing_User()
        {
            var existingUser = new ApplicationUserBuilder()
                .WithEntraObjectId(
                    _currentUser.EntraObjectId)
                .WithEntraTenantId(
                    _currentUser.EntraTenantId)
                .AsApplicationAdministrator()
                .Build();

            _userRepository.Seed(existingUser);

            SeedProfile(
                CreateValidProfile(
                    firstName: "Updated",
                    displayName: "Updated User",
                    isAccountEnabled: false));

            await Assert.ThrowsExactlyAsync<
                UnauthorizedAccessException>(
                () => _handler.HandleAsync());

            Assert.AreEqual(
                "Updated",
                existingUser.FirstName);

            Assert.AreEqual(
                "Updated User",
                existingUser.DisplayName);

            Assert.IsFalse(existingUser.IsActive);

            Assert.AreEqual(
                0,
                _userRepository.AddCallCount);

            Assert.AreEqual(
                1,
                _userRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Register_Login_For_Active_New_User()
        {
            SeedProfile(CreateValidProfile());

            var beforeLogin = DateTime.UtcNow;

            await _handler.HandleAsync();

            var afterLogin = DateTime.UtcNow;

            var createdUser =
                _userRepository.Users.Single();

            Assert.IsNotNull(createdUser.LastLoginAtUtc);

            Assert.IsTrue(
                createdUser.LastLoginAtUtc.Value >=
                beforeLogin);

            Assert.IsTrue(
                createdUser.LastLoginAtUtc.Value <=
                afterLogin);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Register_Login_For_Active_Existing_User()
        {
            var existingUser = new ApplicationUserBuilder()
                .WithEntraObjectId(
                    _currentUser.EntraObjectId)
                .WithEntraTenantId(
                    _currentUser.EntraTenantId)
                .AsApplicationAdministrator()
                .Build();

            _userRepository.Seed(existingUser);
            SeedProfile(CreateValidProfile());

            var beforeLogin = DateTime.UtcNow;

            await _handler.HandleAsync();

            var afterLogin = DateTime.UtcNow;

            Assert.IsNotNull(existingUser.LastLoginAtUtc);

            Assert.IsTrue(
                existingUser.LastLoginAtUtc.Value >=
                beforeLogin);

            Assert.IsTrue(
                existingUser.LastLoginAtUtc.Value <=
                afterLogin);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Normalize_Optional_Profile_Values()
        {
            SeedProfile(
                CreateValidProfile(
                    employeeNumber: " ",
                    firstName: " ",
                    lastName: null,
                    mail: "",
                    department: "\t",
                    jobTitle: null,
                    mobilePhone: " "));

            var result = await _handler.HandleAsync();

            var createdUser =
                _userRepository.Users.Single();

            Assert.IsNull(result.EmployeeNumber);
            Assert.IsNull(result.FirstName);
            Assert.IsNull(result.LastName);

            Assert.IsNull(createdUser.FirstName);
            Assert.IsNull(createdUser.LastName);
            Assert.IsNull(createdUser.Mail);
            Assert.IsNull(createdUser.Department);
            Assert.IsNull(createdUser.JobTitle);
            Assert.IsNull(createdUser.MobilePhone);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Not_Resolve_Employee_Or_Store_For_Global_User()
        {
            SeedProfile(CreateValidProfile());

            await _handler.HandleAsync();

            Assert.AreEqual(
                0,
                _employeeRepository.GetByEmployeeNumberCallCount);

            Assert.AreEqual(
                0,
                _storeRepository.GetByStoreNumberCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_For_Global_User()
        {
            SeedProfile(CreateValidProfile());

            using var cancellationTokenSource =
                new CancellationTokenSource();

            var cancellationToken =
                cancellationTokenSource.Token;

            await _handler.HandleAsync(cancellationToken);

            Assert.AreEqual(
                cancellationToken,
                _entraUserProfileService.LastCancellationToken);

            Assert.AreEqual(
                cancellationToken,
                _userRepository
                    .LastGetByEntraIdentityCancellationToken);

            Assert.AreEqual(
                cancellationToken,
                _userRepository.LastAddCancellationToken);

            Assert.AreEqual(
                cancellationToken,
                _userRepository.LastSaveChangesCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_For_Store_User()
        {
            const string employeeNumber = "103549";
            const string storeNumber = "321";

            _currentUser.Roles =
            [
                ApplicationRole.StoreAdministrator
            ];

            var employee = new EmployeeBuilder()
                .WithEmployeeNumber(employeeNumber)
                .WithStoreNumber(storeNumber)
                .Build();

            var store = new StoreBuilder()
                .WithStoreNumber(storeNumber)
                .Build();

            SeedProfile(
                CreateValidProfile(
                    employeeNumber: employeeNumber));

            _employeeRepository.Seed(employee);
            _storeRepository.Seed(store);

            using var cancellationTokenSource =
                new CancellationTokenSource();

            var cancellationToken =
                cancellationTokenSource.Token;

            await _handler.HandleAsync(cancellationToken);

            Assert.AreEqual(
                cancellationToken,
                _entraUserProfileService.LastCancellationToken);

            Assert.AreEqual(
                cancellationToken,
                _employeeRepository
                    .LastGetByEmployeeNumberCancellationToken);

            Assert.AreEqual(
                cancellationToken,
                _storeRepository
                    .LastGetByStoreNumberCancellationToken);

            Assert.AreEqual(
                cancellationToken,
                _userRepository
                    .LastGetByEntraIdentityCancellationToken);

            Assert.AreEqual(
                cancellationToken,
                _userRepository.LastAddCancellationToken);

            Assert.AreEqual(
                cancellationToken,
                _userRepository.LastSaveChangesCancellationToken);
        }

        private GetOrCreateCurrentUserHandler CreateHandler()
        {
            return new GetOrCreateCurrentUserHandler(
                currentUser: _currentUser,
                userRepository: _userRepository,
                entraUserProfileService:
                    _entraUserProfileService,
                employeeRepository: _employeeRepository,
                storeRepository: _storeRepository);
        }

        private void SeedProfile(
            EntraUserProfile profile)
        {
            _entraUserProfileService.Seed(profile);
        }

        private EntraUserProfile CreateValidProfile(
            Guid? entraObjectId = null,
            string? employeeNumber = "103549",
            string? firstName = "John",
            string? lastName = "Doe",
            string displayName = "John Doe",
            string? mail = "john.doe@example.com",
            string? department = "IT",
            string? jobTitle = "System Administrator",
            string? mobilePhone = "+36 30 123 4567",
            bool isAccountEnabled = true)
        {
            return new EntraUserProfile(
                EntraObjectId:
                    entraObjectId ??
                    _currentUser.EntraObjectId,
                EmployeeNumber: employeeNumber,
                FirstName: firstName,
                LastName: lastName,
                DisplayName: displayName,
                Mail: mail,
                Department: department,
                JobTitle: jobTitle,
                MobilePhone: mobilePhone,
                IsAccountEnabled: isAccountEnabled);
        }
    }
}