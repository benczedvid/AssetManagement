using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Common.Authorization
{
    public static class ApplicationRoleNames
    {
        public const string ApplicationAdministrator = nameof(ApplicationRole.ApplicationAdministrator);
        public const string CentralUser = nameof(ApplicationRole.CentralUser);
        public const string StoreManagement = nameof(ApplicationRole.StoreManagement);
        public const string StoreAdministrator = nameof(ApplicationRole.StoreAdministrator);

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            ApplicationAdministrator,
            CentralUser,
            StoreManagement,
            StoreAdministrator
        };
        public static readonly IReadOnlySet<string> GlobalScopeRoles = new HashSet<string>(StringComparer.Ordinal) {
            ApplicationAdministrator,
            CentralUser
        };
        public static readonly IReadOnlySet<string> StoreScopeRoles = new HashSet<string>(StringComparer.Ordinal) {
            StoreManagement,
            StoreAdministrator
        };
    }
}
