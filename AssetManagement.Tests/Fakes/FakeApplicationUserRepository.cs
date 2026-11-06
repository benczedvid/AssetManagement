using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Tests.Fakes;

internal sealed class FakeApplicationUserRepository: IApplicationUserRepository
{
    private readonly List<ApplicationUser> _users = [];

    public IReadOnlyCollection<ApplicationUser> Users => _users.AsReadOnly();

    public int AddCallCount { get; private set; }
    public int SaveChangesCallCount { get; private set; }
    public int GetByEntraIdentityCallCount { get; private set; }
    public int GetByIdCallCount { get; private set; }

    public CancellationToken LastAddCancellationToken { get; private set; }
    public CancellationToken LastSaveChangesCancellationToken { get; private set; }
    public CancellationToken LastGetByIdCancellationToken { get; private set; }
    public CancellationToken LastGetByEntraIdentityCancellationToken { get; private set; }

    public Task<ApplicationUser?> GetByEntraIdentityAsync(
        Guid entraObjectId,
        Guid entraTenantId,
        CancellationToken cancellationToken = default)
    {
        GetByEntraIdentityCallCount++;
        LastGetByEntraIdentityCancellationToken = cancellationToken;

        var user = _users.SingleOrDefault(user =>
            user.EntraObjectId == entraObjectId &&
            user.EntraTenantId == entraTenantId);

        return Task.FromResult(user);
    }

    public Task<ApplicationUser?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        GetByIdCallCount++;
        LastGetByIdCancellationToken = cancellationToken;

        var user = _users.SingleOrDefault(user => user.Id == id);

        return Task.FromResult(user);
    }

    public Task AddAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        AddCallCount++;
        LastAddCancellationToken = cancellationToken;

        _users.Add(user);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        LastSaveChangesCancellationToken = cancellationToken;

        return Task.CompletedTask;
    }

    public void Seed(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _users.Add(user);
    }

    public void Seed(params ApplicationUser[] users)
    {
        ArgumentNullException.ThrowIfNull(users);

        foreach (var user in users)
        {
            ArgumentNullException.ThrowIfNull(user);
        }

        _users.AddRange(users);
    }
}