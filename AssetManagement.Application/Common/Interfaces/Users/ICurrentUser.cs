using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Common.Interfaces.Users
{
    public interface ICurrentUser
    {
        Guid EntraObjectId { get; }
        Guid EntraTenantId { get; }
        string? FirstName { get; }
        string? LastName { get; }
        string DisplayName { get; }
        IReadOnlyCollection<ApplicationRole> Roles { get; }
        bool IsAuthenticated { get; }
    }
}
