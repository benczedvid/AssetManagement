using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Common.Authorization;

public static class ServiceCaseAuthorization
{
    public static void EnsureCanRead(ApplicationUser currentUser, Guid? originStoreId)
    {
        EnsureActiveUser(currentUser);
        if(HasPermission(currentUser, ApplicationPermissions.ServiceCasesReadAll))
        {
            return;
        }
         if (!HasPermission(currentUser, ApplicationPermissions.ServiceCasesReadOwnStore))
        {
            throw new ServiceCaseAccessDeniedException("The current user does not have permission to view service cases.");
        }
        EnsureOwnStoreAccess(currentUser, originStoreId);
    }
    public static void EnsureCanManage(ApplicationUser currentUser, Guid? originStoreId)
    {
        EnsureActiveUser(currentUser);
        if(HasPermission(currentUser, ApplicationPermissions.ServiceCasesManageAll))
        {
            return;
        }
        if(!HasPermission(currentUser, ApplicationPermissions.ServiceCasesManageOwnStore))
        {
            throw new ServiceCaseAccessDeniedException("The current user does not have permission to manage service cases.");
        }
        EnsureOwnStoreAccess(currentUser, originStoreId);
    }
    public static void EnsureCanManageAll(ApplicationUser currentUser)
    {
        EnsureActiveUser(currentUser);
        if(!HasPermission(currentUser, ApplicationPermissions.ServiceCasesManageAll))
        {
            throw new ServiceCaseAccessDeniedException("The current user does not have permission to perform headquarters service operations.");
        }
    }

    public static void EnsureCanClose(ApplicationUser currentUser)
    {
        EnsureActiveUser(currentUser);
        if(!HasPermission(currentUser, ApplicationPermissions.ServiceCasesClose))
        {
            throw new ServiceCaseAccessDeniedException("The current user does not have permission to close service cases.");
        }
    }

    private static bool HasPermission(ApplicationUser currentUser, string permission)
    {
        return RolePermissions.HasPermission(currentUser.Role.ToString(), permission);
    }
    private static void EnsureActiveUser(ApplicationUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        if (!currentUser.IsActive)
        {
            throw new ServiceCaseAccessDeniedException("The current user is inactive.");
        }
    }
    private static void EnsureOwnStoreAccess(ApplicationUser currentUser, Guid? originStoreId)
    {
        if(!currentUser.StoreId.HasValue || currentUser.StoreId.Value == Guid.Empty)
        {
            throw new ServiceCaseAccessDeniedException("The current user is not assigned to a valid store.");
        }
        if(!originStoreId.HasValue || originStoreId.Value == Guid.Empty)
        {
            throw new ServiceCaseAccessDeniedException("Store-scoped users cannot access a service case without a valid origin store.");
        }
        if(currentUser.StoreId.Value != originStoreId.Value)
        {
            throw new ServiceCaseAccessDeniedException("The current user cannot access a service case belonging to another store.");
        }
    }
}