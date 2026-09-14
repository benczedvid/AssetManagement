using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Common.Models;

namespace AssetManagement.Tests.Fakes
{
    internal sealed class FakeEntraUserProfileService : IEntraUserProfileService
    {
        private readonly Dictionary<Guid, EntraUserProfile> _profiles = [];
        public int GetByObjectIdCallCount { get; private set; }
        public Guid? LastRequestedEntraObjectId { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }
        public Task<EntraUserProfile?> GetByObjectIdAsync(Guid entraObjectId, CancellationToken cancellationToken = default)
        {
            GetByObjectIdCallCount++;
            LastRequestedEntraObjectId = entraObjectId;
            LastCancellationToken = cancellationToken;

            _profiles.TryGetValue(entraObjectId, out var profile);

            return Task.FromResult(profile);
        }

        public void Seed(EntraUserProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            _profiles[profile.EntraObjectId] = profile;
        }

        public void Seed(params EntraUserProfile[] profiles)
        {
            ArgumentNullException.ThrowIfNull(profiles);

            foreach (var profile in profiles)
            {
                Seed(profile);
            }
        }
    }
}