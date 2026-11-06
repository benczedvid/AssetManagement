using AssetManagement.Application.Packages.Exceptions;
using AssetManagement.Domain.Entities.Packages;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Common.Authorization;

/// <summary>
/// Provides authorization rules for package operations.
/// </summary>
public static class PackageAuthorization
{
    /// <summary>
    /// Ensures that the current user can read the package.
    /// </summary>
    /// <param name="currentUser">
    /// The current application user.
    /// </param>
    /// <param name="package">
    /// The package to read.
    /// </param>
    /// <exception cref="PackageAccessDeniedException">
    /// Thrown when the current user cannot read the package.
    /// </exception>
    public static void EnsureCanRead(ApplicationUser currentUser, Package package)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(package);

        EnsureActiveUser(currentUser);

        if (HasPermission(currentUser, ApplicationPermissions.PackagesReadAll))
        {
            return;
        }

        if (!HasPermission(currentUser, ApplicationPermissions.PackagesReadOwnStore))
        {
            throw new PackageAccessDeniedException("The current user does not have permission to view packages.");
        }

        EnsurePackageInvolvesOwnStore(currentUser, package);
    }

    /// <summary>
    /// Ensures that the current user can create a package
    /// from the source store.
    /// </summary>
    public static void EnsureCanCreate(ApplicationUser currentUser, Guid sourceStoreId)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        EnsureActiveUser(currentUser);

        if (HasPermission(currentUser, ApplicationPermissions.PackagesManageAll))
        {
            return;
        }

        EnsureOwnStoreManagementPermission(currentUser);
        EnsureOwnStore(currentUser, sourceStoreId);
    }

    /// <summary>
    /// Ensures that the current user can modify a created package
    /// at its source store.
    /// </summary>
    public static void EnsureCanManageAtSource(ApplicationUser currentUser, Package package)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(package);

        EnsureActiveUser(currentUser);

        if (HasPermission(currentUser, ApplicationPermissions.PackagesManageAll))
        {
            return;
        }

        EnsureOwnStoreManagementPermission(currentUser);
        EnsureOwnStore(currentUser, package.SourceStoreId);
    }

    /// <summary>
    /// Ensures that the current user can receive a package
    /// at its destination store.
    /// </summary>
    public static void EnsureCanReceive(ApplicationUser currentUser, Package package)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(package);

        EnsureActiveUser(currentUser);

        if (HasPermission(currentUser, ApplicationPermissions.PackagesManageAll))
        {
            return;
        }

        EnsureOwnStoreManagementPermission(currentUser);
        EnsureOwnStore(currentUser, package.DestinationStoreId);
    }

    private static void EnsurePackageInvolvesOwnStore(ApplicationUser currentUser, Package package)
    {
        var storeId = GetValidStoreId(currentUser);

        var involvesOwnStore = package.SourceStoreId == storeId || package.DestinationStoreId == storeId;

        if (!involvesOwnStore)
        {
            throw new PackageAccessDeniedException("The current user cannot access a package that does not involve the assigned store.");
        }
    }

    private static void EnsureOwnStore(ApplicationUser currentUser, Guid packageStoreId)
    {
        var userStoreId = GetValidStoreId(currentUser);

        if (userStoreId != packageStoreId)
        {
            throw new PackageAccessDeniedException("The current user cannot perform the package operation for another store.");
        }
    }

    private static void EnsureOwnStoreManagementPermission(ApplicationUser currentUser)
    {
        if (!HasPermission(currentUser, ApplicationPermissions.PackagesManageOwnStore))
        {
            throw new PackageAccessDeniedException("The current user does not have permission to manage packages.");
        }
    }

    private static Guid GetValidStoreId(ApplicationUser currentUser)
    {
        if (!currentUser.StoreId.HasValue || currentUser.StoreId.Value == Guid.Empty)
        {
            throw new PackageAccessDeniedException("The current user is not assigned to a valid store.");
        }

        return currentUser.StoreId.Value;
    }

    private static void EnsureActiveUser(ApplicationUser currentUser)
    {
        if (!currentUser.IsActive)
        {
            throw new PackageAccessDeniedException("The current user is inactive.");
        }
    }

    private static bool HasPermission(ApplicationUser currentUser, string permission)
    {
        return RolePermissions.HasPermission(currentUser.Role.ToString(), permission);
    }
}