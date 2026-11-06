namespace AssetManagement.Domain.Entities.Users
{
    public sealed class ApplicationUser
    {
        public Guid Id { get; private set; }
        public Guid EntraObjectId { get; private set; }
        public Guid EntraTenantId { get; private set; }
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string DisplayName { get; private set; } = null!;
        public ApplicationRole Role { get; private set; }
        public Guid? StoreId { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime? LastLoginAtUtc { get; private set; }
        public string? Mail { get; private set; }
        public string? Department { get; private set; }
        public string? JobTitle { get; private set; }
        public string? MobilePhone { get; private set; }
        private ApplicationUser() {}

        private ApplicationUser(
            Guid entraObjectId,
            Guid entraTenantId,
            string? firstName,
            string? lastName,
            string displayName,
            ApplicationRole role,
            Guid? storeId,
            bool isActive,
            string? mail,
            string? department,
            string? jobTitle,
            string? mobilePhone)
        {
            Id = Guid.NewGuid();
            EntraObjectId = entraObjectId;
            EntraTenantId = entraTenantId;
            FirstName = NormalizeOptional(firstName);
            LastName = NormalizeOptional(lastName);
            DisplayName = displayName.Trim();
            Role = role;
            StoreId = storeId;
            IsActive = isActive;
            LastLoginAtUtc = null;
            Mail = NormalizeOptional(mail);
            Department = NormalizeOptional(department);
            JobTitle = NormalizeOptional(jobTitle);
            MobilePhone = NormalizeOptional(mobilePhone);
        }

        public static ApplicationUser CreateFromEntra(
            Guid entraObjectId,
            Guid entraTenantId,
            string? firstName,
            string? lastName,
            string displayName,
            ApplicationRole role,
            bool isActive,
            string? mail,
            string? department,
            string? jobTitle,
            string? mobilePhone,
            Guid? storeId = null)
        {
            if (entraObjectId == Guid.Empty)
            {
                throw new ArgumentException("The Entra Object ID cannot be empty.", nameof(entraObjectId));
            }

            if (entraTenantId == Guid.Empty)
            {
                throw new ArgumentException("The Entra Tenant ID cannot be empty.", nameof(entraTenantId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            ValidateAccessState(role, isActive, storeId);

            return new ApplicationUser(
                entraObjectId: entraObjectId,
                entraTenantId: entraTenantId,
                firstName: firstName,
                lastName: lastName,
                displayName: displayName,
                role: role,
                storeId: storeId,
                isActive: isActive,
                mail: mail,
                department: department,
                jobTitle: jobTitle,
                mobilePhone: mobilePhone);
        }

        public void SynchronizeProfile(
            string? firstName,
            string? lastName,
            string displayName,
            string? mail,
            string? department,
            string? jobTitle,
            string? mobilePhone)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            FirstName = NormalizeOptional(firstName);
            LastName = NormalizeOptional(lastName);
            DisplayName = displayName.Trim();
            Mail = NormalizeOptional(mail);
            Department = NormalizeOptional(department);
            JobTitle = NormalizeOptional(jobTitle);
            MobilePhone = NormalizeOptional(mobilePhone);
        }

        public void SynchronizeAccess(ApplicationRole role, bool isActive)
        {
            var storeId = IsStoreScopedRole(role) ? StoreId : null;

            ValidateAccessState(role, isActive, storeId, requireStoreAssignment: false);

            Role = role;
            IsActive = isActive;
            StoreId = storeId;
        }

        public void AssignStore(Guid storeId)
        {
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("The store identifier cannot be empty.", nameof(storeId));
            }

            if (!IsStoreScopedRole(Role))
            {
                throw new InvalidOperationException("Only store-scoped users can be assigned to a store.");
            }

            StoreId = storeId;
        }

        public void RemoveStoreAssignment()
        {
            StoreId = null;
        }

        public void RegisterLogin()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("An inactive user cannot register a login.");
            }

            if (IsStoreScopedRole(Role) && StoreId is null)
            {
                throw new InvalidOperationException("A store-scoped user must be assigned to a store.");
            }

            LastLoginAtUtc = DateTime.UtcNow;
        }

        private static void ValidateAccessState(
            ApplicationRole role,
            bool isActive,
            Guid? storeId,
            bool requireStoreAssignment = true)
        {
            if (!Enum.IsDefined(role))
            {
                throw new ArgumentOutOfRangeException(nameof(role), role, "The application role is not valid.");
            }

            if (isActive && role == ApplicationRole.Unknown)
            {
                throw new ArgumentException("An active user must have a valid application role.", nameof(role));
            }

            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("The store identifier cannot be empty.", nameof(storeId));
            }

            if (!IsStoreScopedRole(role) && storeId.HasValue)
            {
                throw new ArgumentException("A global user cannot be assigned to a store.", nameof(storeId));
            }

            if (requireStoreAssignment && isActive && IsStoreScopedRole(role) && !storeId.HasValue)
            {
                throw new ArgumentException("An active store-scoped user must be assigned to a store.", nameof(storeId));
            }
        }

        private static bool IsStoreScopedRole(ApplicationRole role)
        {
            return role == ApplicationRole.StoreManagement || role == ApplicationRole.StoreAdministrator;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}