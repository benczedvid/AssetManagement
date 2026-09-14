using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Users.GetOrCreateCurrentUser
{
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