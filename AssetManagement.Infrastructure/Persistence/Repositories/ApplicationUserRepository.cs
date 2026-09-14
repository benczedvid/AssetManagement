using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories
{
    public sealed class ApplicationUserRepository : IApplicationUserRepository
    {
        private readonly AppDbContext _dbContext;

        public ApplicationUserRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            await _dbContext.ApplicationUsers.AddAsync(user, cancellationToken);
        }

        public Task<ApplicationUser?> GetByEntraIdentityAsync(Guid entraObjectId, Guid entraTenantId, CancellationToken cancellationToken = default)
        {
            return  _dbContext.ApplicationUsers.SingleOrDefaultAsync(user => 
                user.EntraObjectId == entraObjectId &&
                user.EntraTenantId == entraTenantId, cancellationToken);
        }
        public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.ApplicationUsers
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user => user.Id == id,
                    cancellationToken);
        }

        public  Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
