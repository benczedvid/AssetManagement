using AssetManagement.Application.Common.Authorization;

namespace AssetManagement.Tests.Fakes
{
    public sealed class FakeUserAccessScopeResolver
        : IUserAccessScopeResolver
    {
        private UserAccessScope _scope = UserAccessScope.Global();

        public int ResolveCallCount { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public void SetGlobalScope()
        {
            _scope = UserAccessScope.Global();
        }

        public void SetStoreScope(Guid storeId)
        {
            _scope = UserAccessScope.ForStore(storeId);
        }

        public void SetScope(UserAccessScope scope)
        {
            ArgumentNullException.ThrowIfNull(scope);

            _scope = scope;
        }

        public Task<UserAccessScope> ResolveAsync(CancellationToken cancellationToken = default)
        {
            ResolveCallCount++;
            LastCancellationToken = cancellationToken;

            return Task.FromResult(_scope);
        }
    }
}