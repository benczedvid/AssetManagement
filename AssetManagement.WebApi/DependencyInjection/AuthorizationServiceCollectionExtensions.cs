using AssetManagement.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AssetManagement.WebApi.DependencyInjection;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddAssetManagementAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.ViewDashboard,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(new PermissionRequirement(
                            ApplicationPermissions.DashboardReadAll,
                            ApplicationPermissions.DashboardReadOwnStore));
                });

            options.AddPolicy(AuthorizationPolicies.ViewAssets,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(new PermissionRequirement(
                            ApplicationPermissions.AssetsReadAll,
                            ApplicationPermissions.AssetsReadOwnStore));
                });

            options.AddPolicy(AuthorizationPolicies.ViewEmployees,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new PermissionRequirement(
                            ApplicationPermissions.EmployeesReadAll,
                            ApplicationPermissions.EmployeesReadOwnStore));
                });

            options.AddPolicy(AuthorizationPolicies.ManageAssets,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.RequireRole(ApplicationRoleNames.ApplicationAdministrator);
                });

            options.AddPolicy(AuthorizationPolicies.ManageStores,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.RequireRole(
                        ApplicationRoleNames.ApplicationAdministrator,
                        ApplicationRoleNames.HotlineAgent);
                });

            options.AddPolicy(AuthorizationPolicies.ManageUsers,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.RequireRole(ApplicationRoleNames.ApplicationAdministrator);
                });
            options.AddPolicy(AuthorizationPolicies.ManageVendors,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.RequireRole(
                        ApplicationRoleNames.ApplicationAdministrator,
                        ApplicationRoleNames.HotlineAgent);
                });

            options.AddPolicy(
                AuthorizationPolicies.ViewServiceCases,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new PermissionRequirement(
                            ApplicationPermissions.ServiceCasesReadAll,
                            ApplicationPermissions.ServiceCasesReadOwnStore));
                });

            options.AddPolicy(
                AuthorizationPolicies.ManageServiceCases,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new PermissionRequirement(
                            ApplicationPermissions.ServiceCasesManageAll,
                            ApplicationPermissions.ServiceCasesManageOwnStore));
                });

            options.AddPolicy(
                AuthorizationPolicies.ManageAllServiceCases,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new PermissionRequirement(
                            ApplicationPermissions.ServiceCasesManageAll));
                });

            options.AddPolicy(
                AuthorizationPolicies.CloseServiceCases,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new PermissionRequirement(
                            ApplicationPermissions.ServiceCasesClose));
                });
                options.AddPolicy(
                    AuthorizationPolicies.ViewPackages,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();

                        policy.AddRequirements(
                            new PermissionRequirement(
                                ApplicationPermissions.PackagesReadAll,
                                ApplicationPermissions.PackagesReadOwnStore));
                    });

                options.AddPolicy(
                    AuthorizationPolicies.ManagePackages,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();

                        policy.AddRequirements(
                            new PermissionRequirement(
                                ApplicationPermissions.PackagesManageAll,
                                ApplicationPermissions.PackagesManageOwnStore));
                    });
        });

        services.AddScoped<
            IAuthorizationHandler,
            PermissionAuthorizationHandler>();

        return services;
    }
}