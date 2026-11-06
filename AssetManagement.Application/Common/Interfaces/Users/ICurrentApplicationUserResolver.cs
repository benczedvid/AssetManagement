using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Common.Interfaces.Users
{
    public interface ICurrentApplicationUserResolver
    {
        Task<ApplicationUser> ResolveAsync(CancellationToken cancellationToken = default);
    }
}