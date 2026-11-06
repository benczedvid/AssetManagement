namespace AssetManagement.Application.Common.Authorization;

public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>>
        PermissionsByRole =
            new Dictionary<string, IReadOnlySet<string>>(
                StringComparer.Ordinal)
            {
                [ApplicationRoleNames.ApplicationAdministrator] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        ApplicationPermissions.DashboardReadAll,
                        ApplicationPermissions.AssetsReadAll,
                        ApplicationPermissions.EmployeesReadAll,
                        ApplicationPermissions.ServiceCasesReadAll,
                        ApplicationPermissions.ServiceCasesManageAll,
                        ApplicationPermissions.ServiceCasesClose,
                        ApplicationPermissions.PackagesReadAll,
                        ApplicationPermissions.PackagesManageAll
                    },

                [ApplicationRoleNames.HotlineAgent] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        ApplicationPermissions.DashboardReadAll,
                        ApplicationPermissions.AssetsReadAll,
                        ApplicationPermissions.ServiceCasesManageAll,
                        ApplicationPermissions.ServiceCasesReadAll,
                        ApplicationPermissions.PackagesReadAll,
                        ApplicationPermissions.PackagesManageAll
                    },

                [ApplicationRoleNames.CentralUser] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        ApplicationPermissions.DashboardReadAll,
                        ApplicationPermissions.AssetsReadAll,
                        ApplicationPermissions.ServiceCasesReadAll,
                        ApplicationPermissions.PackagesReadAll,
                    },

                [ApplicationRoleNames.StoreManagement] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        ApplicationPermissions.DashboardReadOwnStore,
                        ApplicationPermissions.AssetsReadOwnStore,
                        ApplicationPermissions.EmployeesReadOwnStore,
                        ApplicationPermissions.ServiceCasesManageOwnStore,
                        ApplicationPermissions.ServiceCasesReadOwnStore,
                        ApplicationPermissions.PackagesReadOwnStore,
                        ApplicationPermissions.PackagesManageOwnStore
                    },

                [ApplicationRoleNames.StoreAdministrator] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        ApplicationPermissions.DashboardReadOwnStore,
                        ApplicationPermissions.AssetsReadOwnStore,
                        ApplicationPermissions.EmployeesReadOwnStore,
                        ApplicationPermissions.ServiceCasesManageOwnStore,
                        ApplicationPermissions.ServiceCasesReadOwnStore,
                        ApplicationPermissions.PackagesReadOwnStore,
                        ApplicationPermissions.PackagesManageOwnStore
                    }
                    
            };

    public static IReadOnlySet<string> GetPermissions(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        return PermissionsByRole.TryGetValue(
            roleName,
            out var permissions)
                ? permissions
                : new HashSet<string>(StringComparer.Ordinal);
    }

    public static bool HasPermission(
        string roleName,
        string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return GetPermissions(roleName).Contains(permission);
    }

    public static IReadOnlySet<string> GetPermissions(
        IEnumerable<string> roleNames)
    {
        ArgumentNullException.ThrowIfNull(roleNames);

        var permissions = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var roleName in roleNames)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                continue;
            }

            permissions.UnionWith(GetPermissions(roleName));
        }

        return permissions;
    }

    public static bool HasPermission(
        IEnumerable<string> roleNames,
        string permission)
    {
        ArgumentNullException.ThrowIfNull(roleNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return roleNames.Any(
            roleName =>
                !string.IsNullOrWhiteSpace(roleName) &&
                HasPermission(roleName, permission));
    }
}