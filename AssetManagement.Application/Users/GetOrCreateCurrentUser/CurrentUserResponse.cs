using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Users.GetOrCreateCurrentUser
{
    /// <summary>
    /// Represents information about the current authenticated application user.
    /// </summary>
    /// <param name="Id">The unique identifier of the application user.</param>
    /// <param name="EntraObjectId">The unique Microsoft Entra ID object identifier of the user.</param>
    /// <param name="EntraTenantId">The unique Microsoft Entra ID tenant identifier of the user.</param>
    /// <param name="EmployeeNumber">The employee number associated with the user, when available.</param>
    /// <param name="FirstName">The first name of the user, when available.</param>
    /// <param name="LastName">The last name of the user, when available.</param>
    /// <param name="DisplayName">The display name of the user.</param>
    /// <param name="Role">The application role assigned to the user.</param>
    /// <param name="StoreId">The unique identifier of the store assigned to the user, when applicable.</param>
    /// <param name="CanAccessAllStores">Indicates whether the user is allowed to access data from all stores.</param>
    /// <param name="IsActive">Indicates whether the application user account is currently active.</param>
    public sealed record CurrentUserResponse(
        Guid Id,
        Guid EntraObjectId,
        Guid EntraTenantId,
        string? EmployeeNumber,
        string? FirstName,
        string? LastName,
        string DisplayName,
        ApplicationRole Role,
        Guid? StoreId,
        bool CanAccessAllStores,
        bool IsActive);
}