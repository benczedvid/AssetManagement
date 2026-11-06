using AssetManagement.Domain.Entities.Users;
using AssetManagement.Tests.Builders;

namespace AssetManagement.Tests.Domain.Users
{
    [TestClass]
    public sealed class ApplicationUserTests
    {
        [TestMethod]
        public void CreateFromEntra_Should_Create_Active_User_With_Valid_Data()
        {
            var entraObjectId = Guid.NewGuid();
            var entraTenantId = Guid.NewGuid();

            const string firstName = "John";
            const string lastName = "Doe";
            const string displayName = "John Doe";
            const string mail = "john.doe@example.com";
            const string department = "IT";
            const string jobTitle = "System Administrator";
            const string mobilePhone = "+36 30 123 4567";

            var user = new ApplicationUserBuilder()
                .WithEntraObjectId(entraObjectId)
                .WithEntraTenantId(entraTenantId)
                .WithFirstName(firstName)
                .WithLastName(lastName)
                .WithDisplayName(displayName)
                .WithMail(mail)
                .WithDepartment(department)
                .WithJobTitle(jobTitle)
                .WithMobilePhone(mobilePhone)
                .Build();

            Assert.AreNotEqual(Guid.Empty, user.Id);
            Assert.AreEqual(entraObjectId, user.EntraObjectId);
            Assert.AreEqual(entraTenantId, user.EntraTenantId);
            Assert.AreEqual(firstName, user.FirstName);
            Assert.AreEqual(lastName, user.LastName);
            Assert.AreEqual(displayName, user.DisplayName);
            Assert.AreEqual(ApplicationRole.ApplicationAdministrator, user.Role);
            Assert.IsNull(user.StoreId);
            Assert.IsTrue(user.IsActive);
            Assert.IsNull(user.LastLoginAtUtc);
            Assert.AreEqual(mail, user.Mail);
            Assert.AreEqual(department, user.Department);
            Assert.AreEqual(jobTitle, user.JobTitle);
            Assert.AreEqual(mobilePhone, user.MobilePhone);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Generate_Unique_Id()
        {
            var firstUser = new ApplicationUserBuilder().Build();
            var secondUser = new ApplicationUserBuilder().Build();

            Assert.AreNotEqual(Guid.Empty, firstUser.Id);
            Assert.AreNotEqual(Guid.Empty, secondUser.Id);
            Assert.AreNotEqual(firstUser.Id, secondUser.Id);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Throw_When_EntraObjectId_Is_Empty()
        {
            var builder = new ApplicationUserBuilder().WithEntraObjectId(Guid.Empty);
            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("entraObjectId", exception.ParamName);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Throw_When_EntraTenantId_Is_Empty()
        {
            var builder = new ApplicationUserBuilder()
                .WithEntraTenantId(Guid.Empty);

            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => builder.Build());

            Assert.AreEqual("entraTenantId", exception.ParamName);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void CreateFromEntra_Should_Normalize_FirstName_To_Null(string? firstName)
        {
            var user = new ApplicationUserBuilder()
                .WithFirstName(firstName)
                .Build();

            Assert.IsNull(user.FirstName);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Trim_FirstName()
        {
            var user = new ApplicationUserBuilder()
                .WithFirstName("  John  ")
                .Build();

            Assert.AreEqual("John", user.FirstName);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void CreateFromEntra_Should_Normalize_LastName_To_Null(string? lastName)
        {
            var user = new ApplicationUserBuilder()
                .WithLastName(lastName)
                .Build();

            Assert.IsNull(user.LastName);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Trim_LastName()
        {
            var user = new ApplicationUserBuilder()
                .WithLastName("  Doe  ")
                .Build();

            Assert.AreEqual("Doe", user.LastName);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Throw_When_DisplayName_Is_Null()
        {
            var builder = new ApplicationUserBuilder()
                .WithDisplayName(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("displayName", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void CreateFromEntra_Should_Throw_When_DisplayName_Is_Whitespace(string displayName)
        {
            var builder = new ApplicationUserBuilder()
                .WithDisplayName(displayName);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("displayName", exception.ParamName);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Trim_DisplayName()
        {
            var user = new ApplicationUserBuilder()
                .WithDisplayName("  John Doe  ")
                .Build();

            Assert.AreEqual("John Doe", user.DisplayName);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Normalize_Optional_Profile_Values()
        {
            var user = new ApplicationUserBuilder()
                .WithMail("  john.doe@example.com  ")
                .WithDepartment("  IT  ")
                .WithJobTitle("  System Administrator  ")
                .WithMobilePhone("  +36 30 123 4567  ")
                .Build();

            Assert.AreEqual("john.doe@example.com", user.Mail);
            Assert.AreEqual("IT", user.Department);
            Assert.AreEqual("System Administrator", user.JobTitle);
            Assert.AreEqual("+36 30 123 4567", user.MobilePhone);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Normalize_Empty_Profile_Values_To_Null()
        {
            var user = new ApplicationUserBuilder()
                .WithMail(" ")
                .WithDepartment("")
                .WithJobTitle("\t")
                .WithMobilePhone(null)
                .Build();

            Assert.IsNull(user.Mail);
            Assert.IsNull(user.Department);
            Assert.IsNull(user.JobTitle);
            Assert.IsNull(user.MobilePhone);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Throw_When_Role_Is_Not_Defined()
        {
            var invalidRole = (ApplicationRole)999;

            var builder = new ApplicationUserBuilder()
                .WithRole(invalidRole);

            var exception =Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.Build());

            Assert.AreEqual("role", exception.ParamName);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Throw_When_Active_User_Has_Unknown_Role()
        {
            var builder = new ApplicationUserBuilder()
                .WithRole(ApplicationRole.Unknown)
                .AsActive();

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("role", exception.ParamName);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Allow_Inactive_User_With_Unknown_Role()
        {
            var user = new ApplicationUserBuilder()
                .WithRole(ApplicationRole.Unknown)
                .AsInactive()
                .Build();

            Assert.AreEqual(ApplicationRole.Unknown, user.Role);
            Assert.IsFalse(user.IsActive);
            Assert.IsNull(user.StoreId);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Create_StoreAdministrator_With_Store()
        {
            var storeId = Guid.NewGuid();

            var user = new ApplicationUserBuilder()
                .AsStoreAdministrator(storeId)
                .Build();

            Assert.AreEqual(ApplicationRole.StoreAdministrator, user.Role);
            Assert.AreEqual(storeId, user.StoreId);
            Assert.IsTrue(user.IsActive);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Create_StoreManagement_With_Store()
        {
            var storeId = Guid.NewGuid();

            var user = new ApplicationUserBuilder()
                .AsStoreManagement(storeId)
                .Build();

            Assert.AreEqual(ApplicationRole.StoreManagement, user.Role);
            Assert.AreEqual(storeId, user.StoreId);
            Assert.IsTrue(user.IsActive);
        }

        [TestMethod]
        [DataRow(ApplicationRole.StoreManagement)]
        [DataRow(ApplicationRole.StoreAdministrator)]
        public void CreateFromEntra_Should_Throw_When_Active_StoreUser_Has_No_Store(ApplicationRole role)
        {
            var builder = new ApplicationUserBuilder()
                .WithRole(role)
                .WithoutStore()
                .AsActive();

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("storeId", exception.ParamName);
        }

        [TestMethod]
        [DataRow(ApplicationRole.StoreManagement)]
        [DataRow(ApplicationRole.StoreAdministrator)]
        public void CreateFromEntra_Should_Allow_Inactive_StoreUser_Without_Store(ApplicationRole role)
        {
            var user = new ApplicationUserBuilder()
                .WithRole(role)
                .WithoutStore()
                .AsInactive()
                .Build();

            Assert.AreEqual(role, user.Role);
            Assert.IsFalse(user.IsActive);
            Assert.IsNull(user.StoreId);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Throw_When_StoreId_Is_Empty()
        {
            var builder = new ApplicationUserBuilder()
                .WithRole(ApplicationRole.StoreAdministrator)
                .WithStoreId(Guid.Empty);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("storeId", exception.ParamName);
        }

        [TestMethod]
        public void CreateFromEntra_Should_Throw_When_Global_User_Has_Store()
        {
            var builder = new ApplicationUserBuilder()
                .AsApplicationAdministrator()
                .WithStoreId(Guid.NewGuid());

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("storeId", exception.ParamName);
        }

        [TestMethod]
        public void SynchronizeProfile_Should_Update_And_Normalize_Profile()
        {
            var user = new ApplicationUserBuilder().Build();

            user.SynchronizeProfile(
                firstName: "  Jane  ",
                lastName: "  Smith  ",
                displayName: "  Jane Smith  ",
                mail: "  jane.smith@example.com  ",
                department: "  Finance  ",
                jobTitle: "  Financial Analyst  ",
                mobilePhone: "  +36 30 765 4321  ");

            Assert.AreEqual("Jane", user.FirstName);
            Assert.AreEqual("Smith", user.LastName);
            Assert.AreEqual("Jane Smith", user.DisplayName);
            Assert.AreEqual("jane.smith@example.com", user.Mail);
            Assert.AreEqual("Finance", user.Department);
            Assert.AreEqual("Financial Analyst", user.JobTitle);
            Assert.AreEqual("+36 30 765 4321", user.MobilePhone);
        }

        [TestMethod]
        public void SynchronizeProfile_Should_Normalize_Optional_Values_To_Null()
        {
            var user = new ApplicationUserBuilder().Build();

            user.SynchronizeProfile(
                firstName: null,
                lastName: " ",
                displayName: "John Doe",
                mail: "",
                department: "\t",
                jobTitle: null,
                mobilePhone: "  ");

            Assert.IsNull(user.FirstName);
            Assert.IsNull(user.LastName);
            Assert.AreEqual("John Doe", user.DisplayName);
            Assert.IsNull(user.Mail);
            Assert.IsNull(user.Department);
            Assert.IsNull(user.JobTitle);
            Assert.IsNull(user.MobilePhone);
        }

        [TestMethod]
        public void SynchronizeProfile_Should_Throw_When_DisplayName_Is_Null()
        {
            var user = new ApplicationUserBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => user.SynchronizeProfile(
                    firstName: "Jane",
                    lastName: "Smith",
                    displayName: null!,
                    mail: null,
                    department: null,
                    jobTitle: null,
                    mobilePhone: null));

            Assert.AreEqual("displayName", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void SynchronizeProfile_Should_Throw_When_DisplayName_Is_Whitespace(string displayName)
        {
            var user = new ApplicationUserBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => user.SynchronizeProfile(
                    firstName: "Jane",
                    lastName: "Smith",
                    displayName: displayName,
                    mail: null,
                    department: null,
                    jobTitle: null,
                    mobilePhone: null));

            Assert.AreEqual("displayName", exception.ParamName);
        }

        [TestMethod]
        public void SynchronizeAccess_Should_Update_Global_Role_And_Active_State()
        {
            var user = new ApplicationUserBuilder()
                .AsInactive()
                .Build();

            user.SynchronizeAccess(ApplicationRole.CentralUser, isActive: true);

            Assert.AreEqual(ApplicationRole.CentralUser, user.Role);
            Assert.IsTrue(user.IsActive);
            Assert.IsNull(user.StoreId);
        }

        [TestMethod]
        public void SynchronizeAccess_Should_Remove_Store_For_Global_Role()
        {
            var storeId = Guid.NewGuid();

            var user = new ApplicationUserBuilder()
                .AsStoreAdministrator(storeId)
                .Build();

            user.SynchronizeAccess(ApplicationRole.ApplicationAdministrator, isActive: true);

            Assert.AreEqual(ApplicationRole.ApplicationAdministrator, user.Role);
            Assert.IsTrue(user.IsActive);
            Assert.IsNull(user.StoreId);
        }

        [TestMethod]
        public void SynchronizeAccess_Should_Preserve_Store_For_StoreScoped_Role()
        {
            var storeId = Guid.NewGuid();

            var user = new ApplicationUserBuilder()
                .AsStoreAdministrator(storeId)
                .Build();

            user.SynchronizeAccess(ApplicationRole.StoreManagement, isActive: true);

            Assert.AreEqual(ApplicationRole.StoreManagement, user.Role);
            Assert.IsTrue(user.IsActive);
            Assert.AreEqual(storeId, user.StoreId);
        }

        [TestMethod]
        public void SynchronizeAccess_Should_Allow_StoreRole_Without_Store()
        {
            var user = new ApplicationUserBuilder().Build();

            user.SynchronizeAccess(ApplicationRole.StoreAdministrator, isActive: true);

            Assert.AreEqual(ApplicationRole.StoreAdministrator, user.Role);
            Assert.IsTrue(user.IsActive);
            Assert.IsNull(user.StoreId);
        }

        [TestMethod]
        public void SynchronizeAccess_Should_Throw_When_Role_Is_Not_Defined()
        {
            var user = new ApplicationUserBuilder().Build();
            var invalidRole = (ApplicationRole)999;
            var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => user.SynchronizeAccess(invalidRole, isActive: true));

            Assert.AreEqual("role", exception.ParamName);
        }

        [TestMethod]
        public void SynchronizeAccess_Should_Throw_When_Active_User_Has_Unknown_Role()
        {
            var user = new ApplicationUserBuilder().Build();
            var exception = Assert.ThrowsExactly<ArgumentException>(() => user.SynchronizeAccess(ApplicationRole.Unknown, isActive: true));

            Assert.AreEqual("role", exception.ParamName);
        }

        [TestMethod]
        public void AssignStore_Should_Assign_Store_To_StoreScoped_User()
        {
            var storeId = Guid.NewGuid();

            var user = new ApplicationUserBuilder()
                .WithRole(ApplicationRole.StoreAdministrator)
                .WithoutStore()
                .AsInactive()
                .Build();

            user.AssignStore(storeId);

            Assert.AreEqual(storeId, user.StoreId);
        }

        [TestMethod]
        public void AssignStore_Should_Replace_Existing_Store()
        {
            var originalStoreId = Guid.NewGuid();
            var newStoreId = Guid.NewGuid();

            var user = new ApplicationUserBuilder()
                .AsStoreAdministrator(originalStoreId)
                .Build();

            user.AssignStore(newStoreId);

            Assert.AreEqual(newStoreId, user.StoreId);
        }

        [TestMethod]
        public void AssignStore_Should_Throw_When_StoreId_Is_Empty()
        {
            var user = new ApplicationUserBuilder()
                .AsStoreAdministrator(Guid.NewGuid())
                .Build();
            var exception = Assert.ThrowsExactly<ArgumentException>(() => user.AssignStore(Guid.Empty));

            Assert.AreEqual("storeId", exception.ParamName);
        }

        [TestMethod]
        public void AssignStore_Should_Throw_For_Global_User()
        {
            var user = new ApplicationUserBuilder().Build();

            Assert.ThrowsExactly<InvalidOperationException>(() => user.AssignStore(Guid.NewGuid()));
        }

        [TestMethod]
        public void RemoveStoreAssignment_Should_Remove_Assigned_Store()
        {
            var user = new ApplicationUserBuilder()
                .AsStoreAdministrator(Guid.NewGuid())
                .Build();

            user.RemoveStoreAssignment();

            Assert.IsNull(user.StoreId);
        }

        [TestMethod]
        public void RemoveStoreAssignment_Should_Keep_Null_Without_Assignment()
        {
            var user = new ApplicationUserBuilder().Build();

            user.RemoveStoreAssignment();

            Assert.IsNull(user.StoreId);
        }

        [TestMethod]
        public void RegisterLogin_Should_Set_LastLoginAtUtc_For_Active_Global_User()
        {
            var user = new ApplicationUserBuilder().Build();
            var beforeLogin = DateTime.UtcNow;

            user.RegisterLogin();

            var afterLogin = DateTime.UtcNow;

            Assert.IsNotNull(user.LastLoginAtUtc);
            Assert.IsTrue(user.LastLoginAtUtc.Value >= beforeLogin);
            Assert.IsTrue(user.LastLoginAtUtc.Value <= afterLogin);
        }

        [TestMethod]
        public void RegisterLogin_Should_Set_LastLoginAtUtc_For_StoreUser()
        {
            var user = new ApplicationUserBuilder()
                .AsStoreAdministrator(Guid.NewGuid())
                .Build();

            user.RegisterLogin();

            Assert.IsNotNull(user.LastLoginAtUtc);
        }

        [TestMethod]
        public void RegisterLogin_Should_Throw_For_Inactive_User()
        {
            var user = new ApplicationUserBuilder()
                .AsInactive()
                .Build();

            Assert.ThrowsExactly<InvalidOperationException>(() => user.RegisterLogin());
            Assert.IsNull(user.LastLoginAtUtc);
        }

        [TestMethod]
        public void RegisterLogin_Should_Throw_For_StoreUser_Without_Store()
        {
            var user = new ApplicationUserBuilder().Build();

            user.SynchronizeAccess(ApplicationRole.StoreAdministrator, isActive: true);

            Assert.ThrowsExactly<InvalidOperationException>(() => user.RegisterLogin());
            Assert.IsNull(user.LastLoginAtUtc);
        }
    }
}