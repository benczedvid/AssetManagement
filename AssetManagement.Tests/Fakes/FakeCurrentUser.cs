using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Tests.Fakes
{
    internal sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid EntraObjectId { get; set; } = Guid.NewGuid();
        public Guid EntraTenantId { get; set; } = Guid.NewGuid();
        public string FirstName { get; set; } = "John";
        public string LastName { get; set; } = "Doe";
        public string DisplayName { get; set; } = "John Doe";
        public bool IsAuthenticated { get; set; } = true;

        public IReadOnlyCollection<ApplicationRole> Roles
        {
            get;
            set;
        } =
        [
            ApplicationRole.ApplicationAdministrator
        ];
    }
}