using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Common.Interfaces.Users
{
    public interface IApplicationUserRepository
    {
        Task<ApplicationUser?>GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<ApplicationUser?> GetByEntraIdentityAsync(Guid entraObjectId, Guid entraTenantId, CancellationToken cancellationToken = default);
        Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
