using Microsoft.AspNetCore.Authorization;

namespace AssetManagement.Application.Common.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var roleNames = ApplicationRoleNames.All
            .Where(context.User.IsInRole)
            .ToArray();

        if (roleNames.Length == 0)
        {
            return Task.CompletedTask;
        }

        var assignedPermissions =
            RolePermissions.GetPermissions(roleNames);

        var hasRequiredPermission =
            requirement.Permissions.Any(
                assignedPermissions.Contains);

        if (hasRequiredPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}