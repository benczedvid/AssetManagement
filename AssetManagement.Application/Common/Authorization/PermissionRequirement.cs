using Microsoft.AspNetCore.Authorization;

namespace AssetManagement.Application.Common.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        if (permissions.Length == 0)
        {
            throw new ArgumentException(
                "At least one permission is required.",
                nameof(permissions));
        }

        if (permissions.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Permission names cannot be empty.",
                nameof(permissions));
        }

        var unknownPermissions = permissions
            .Where(permission =>
                !ApplicationPermissions.All.Contains(permission))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (unknownPermissions.Length > 0)
        {
            throw new ArgumentException(
                $"The following permissions are not registered: " +
                $"{string.Join(", ", unknownPermissions)}.",
                nameof(permissions));
        }

        Permissions = permissions
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyCollection<string> Permissions { get; }
}