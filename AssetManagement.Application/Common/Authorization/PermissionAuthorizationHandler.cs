using AssetManagement.Application.Common.Interfaces.Users;
using Microsoft.AspNetCore.Authorization;

namespace AssetManagement.Application.Common.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentApplicationUserResolver
        _currentApplicationUserResolver;

    public PermissionAuthorizationHandler(ICurrentApplicationUserResolver currentApplicationUserResolver)
    {
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);

        _currentApplicationUserResolver = currentApplicationUserResolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var currentUser = await _currentApplicationUserResolver.ResolveAsync();

        if (!currentUser.IsActive)
        {
            return;
        }

        var roleName = currentUser.Role.ToString();

        var assignedPermissions = RolePermissions.GetPermissions(roleName);

        var hasRequiredPermission = requirement.Permissions.Any(assignedPermissions.Contains);

        if (hasRequiredPermission)
        {
            context.Succeed(requirement);
        }
    }
}