using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Tests.Builders
{
    public sealed class ApplicationUserBuilder
    {
        private Guid _entraObjectId = Guid.NewGuid();
        private Guid _entraTenantId = Guid.NewGuid();
        private string? _firstName = "John";
        private string? _lastName = "Doe";
        private string? _displayName = "John Doe";
        private ApplicationRole _role = ApplicationRole.ApplicationAdministrator;
        private bool _isActive = true;
        private string? _mail = "john.doe@example.com";
        private string? _department = "IT";
        private string? _jobTitle = "System Administrator";
        private string? _mobilePhone = "+36 30 123 4567";
        private Guid? _storeId;

        public ApplicationUserBuilder WithEntraObjectId(Guid entraObjectId) { _entraObjectId = entraObjectId; return this; }
        public ApplicationUserBuilder WithEntraTenantId(Guid entraTenantId) { _entraTenantId = entraTenantId; return this; }
        public ApplicationUserBuilder WithFirstName(string? firstName) { _firstName = firstName; return this; }
        public ApplicationUserBuilder WithLastName(string? lastName) { _lastName = lastName; return this; }
        public ApplicationUserBuilder WithDisplayName(string? displayName) { _displayName = displayName; return this; }
        public ApplicationUserBuilder WithRole(ApplicationRole role) { _role = role; return this; }
        public ApplicationUserBuilder WithIsActive(bool isActive) { _isActive = isActive; return this; }
        public ApplicationUserBuilder AsActive() { _isActive = true; return this; }
        public ApplicationUserBuilder AsInactive() { _isActive = false; return this; }
        public ApplicationUserBuilder WithMail(string? mail) { _mail = mail; return this; }
        public ApplicationUserBuilder WithDepartment(string? department) { _department = department; return this; }
        public ApplicationUserBuilder WithJobTitle(string? jobTitle) { _jobTitle = jobTitle;  return this; }
        public ApplicationUserBuilder WithMobilePhone(string? mobilePhone) { _mobilePhone = mobilePhone; return this; }
        public ApplicationUserBuilder WithStoreId(Guid? storeId) { _storeId = storeId; return this; }
        public ApplicationUserBuilder WithoutStore() { _storeId = null; return this; }
        public ApplicationUserBuilder AsApplicationAdministrator() { _role = ApplicationRole.ApplicationAdministrator; _storeId = null; return this; }
        public ApplicationUserBuilder AsCentralUser() { _role = ApplicationRole.CentralUser; _storeId = null; return this; }
        public ApplicationUserBuilder AsStoreManagement(Guid storeId) { _role = ApplicationRole.StoreManagement; _storeId = storeId; return this; }
        public ApplicationUserBuilder AsStoreAdministrator(Guid storeId) { _role = ApplicationRole.StoreAdministrator; _storeId = storeId; return this; }

        public ApplicationUser Build()
        {
            return ApplicationUser.CreateFromEntra(
                entraObjectId: _entraObjectId,
                entraTenantId: _entraTenantId,
                firstName: _firstName,
                lastName: _lastName,
                displayName: _displayName!,
                role: _role,
                isActive: _isActive,
                mail: _mail,
                department: _department,
                jobTitle: _jobTitle,
                mobilePhone: _mobilePhone,
                storeId: _storeId);
        }
    }
}