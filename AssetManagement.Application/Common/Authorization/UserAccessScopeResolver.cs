using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Common.Authorization;

public sealed class UserAccessScopeResolver : IUserAccessScopeResolver
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationUserRepository _applicationUserRepository;

    public UserAccessScopeResolver(
        ICurrentUser currentUser,
        IApplicationUserRepository applicationUserRepository)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(applicationUserRepository);

        _currentUser = currentUser;
        _applicationUserRepository = applicationUserRepository;
    }

    public async Task<UserAccessScope> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("The current user is not authenticated.");
        }

        if (_currentUser.Roles.Count == 0)
        {
            throw new UnauthorizedAccessException("The current user has no application role.");
        }

        var applicationUser =
            await _applicationUserRepository
                .GetByEntraIdentityAsync(
                    entraObjectId: _currentUser.EntraObjectId,
                    entraTenantId: _currentUser.EntraTenantId,
                    cancellationToken: cancellationToken);

        if (applicationUser is null)
        {
            throw new UnauthorizedAccessException("The current user is not registered in the application.");
        }

        if (!applicationUser.IsActive)
        {
            throw new UnauthorizedAccessException("The current user is inactive.");
        }

        if (HasGlobalScopeRole(_currentUser.Roles))
        {
            return UserAccessScope.Global();
        }

        if (HasStoreScopeRole(_currentUser.Roles))
        {
            if (!applicationUser.StoreId.HasValue)
            {
                throw new InvalidOperationException("The current store-scoped user is not assigned to a store.");
            }

            return UserAccessScope.ForStore(applicationUser.StoreId.Value);
        }

        throw new UnauthorizedAccessException("The current user has no supported access scope.");
    }

    private static bool HasGlobalScopeRole(IReadOnlyCollection<ApplicationRole> roles)
    {
        return roles.Contains(ApplicationRole.ApplicationAdministrator) || roles.Contains(ApplicationRole.CentralUser);
    }

    private static bool HasStoreScopeRole(IReadOnlyCollection<ApplicationRole> roles)
    {
        return roles.Contains(ApplicationRole.StoreManagement) || roles.Contains(ApplicationRole.StoreAdministrator);
    }
}