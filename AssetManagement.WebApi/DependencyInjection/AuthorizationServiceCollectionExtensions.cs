using AssetManagement.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AssetManagement.WebApi.DependencyInjection;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddAssetManagementAuthorization(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.ViewDashboard,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new PermissionRequirement(
                            ApplicationPermissions.DashboardReadAll,
                            ApplicationPermissions.DashboardReadOwnStore));
                });

            options.AddPolicy(
                AuthorizationPolicies.ViewAssets,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new PermissionRequirement(
                            ApplicationPermissions.AssetsReadAll,
                            ApplicationPermissions.AssetsReadOwnStore));
                });

            options.AddPolicy(
                AuthorizationPolicies.ViewEmployees,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new PermissionRequirement(
                            ApplicationPermissions.EmployeesReadAll,
                            ApplicationPermissions.EmployeesReadOwnStore));
                });

            options.AddPolicy(
                AuthorizationPolicies.ManageAssets,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.RequireRole(
                        ApplicationRoleNames.ApplicationAdministrator);
                });

            options.AddPolicy(
                AuthorizationPolicies.ManageStores,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.RequireRole(
                        ApplicationRoleNames.ApplicationAdministrator);
                });

            options.AddPolicy(
                AuthorizationPolicies.ManageUsers,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.RequireRole(
                        ApplicationRoleNames.ApplicationAdministrator);
                });
        });

        services.AddSingleton<
            IAuthorizationHandler,
            PermissionAuthorizationHandler>();

        return services;
    }
}