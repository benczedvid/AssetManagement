namespace AssetManagement.Application.Common.Authorization;

public interface IUserAccessScopeResolver
{
    Task<UserAccessScope> ResolveAsync(CancellationToken cancellationToken = default);
}