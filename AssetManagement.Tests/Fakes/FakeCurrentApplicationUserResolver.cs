using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Tests.Fakes
{
    public sealed class FakeCurrentApplicationUserResolver : ICurrentApplicationUserResolver
    {
        private ApplicationUser? _currentUser;
        public int ResolveCallCount { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }
        public void SetCurrentUser(ApplicationUser currentUser)
        {
            ArgumentNullException.ThrowIfNull(currentUser);

            _currentUser = currentUser;
        }

        public Task<ApplicationUser> ResolveAsync(CancellationToken cancellationToken = default)
        {
            ResolveCallCount++;
            LastCancellationToken = cancellationToken;

            if (_currentUser is null)
            {
                throw new InvalidOperationException("The current application user has not been configured.");
            }

            return Task.FromResult(_currentUser);
        }
    }
}