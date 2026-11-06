
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssetManagement.Infrastructure.Identity
{
    public sealed class CurrentApplicationUserResolver : ICurrentApplicationUserResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _dbContext;

        public CurrentApplicationUserResolver(IHttpContextAccessor httpContextAccessor, AppDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(httpContextAccessor);
            ArgumentNullException.ThrowIfNull(dbContext);

            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
        }

        public async Task<ApplicationUser> ResolveAsync(CancellationToken cancellationToken = default)
        {
            var principal = _httpContextAccessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException("The current user is not authenticated.");
            }

            var objectIdValue =
                principal.FindFirst("oid")?.Value
                ?? principal.FindFirst(
                    "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (!Guid.TryParse(objectIdValue, out var entraObjectId))
            {
                throw new UnauthorizedAccessException("The authenticated user does not have a valid Entra Object ID.");
            }

            var applicationUser = await _dbContext.ApplicationUsers
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user => user.EntraObjectId == entraObjectId,
                    cancellationToken);

            if (applicationUser is null)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user does not exist in the application.");
            }

            if (!applicationUser.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user is inactive.");
            }

            return applicationUser;
        }
    }
}