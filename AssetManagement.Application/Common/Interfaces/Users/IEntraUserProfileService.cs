using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Common.Interfaces.Users;

public interface IEntraUserProfileService
{
    Task<EntraUserProfile?> GetByObjectIdAsync(Guid entraObjectId, CancellationToken cancellationToken = default);
}