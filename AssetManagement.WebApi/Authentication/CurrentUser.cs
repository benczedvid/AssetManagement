using System.Security.Claims;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.WebApi.Authentication;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public Guid EntraObjectId => GetRequiredGuidClaim(EntraClaimTypes.ObjectId, "http://schemas.microsoft.com/identity/claims/objectidentifier");
    public Guid EntraTenantId => GetRequiredGuidClaim(EntraClaimTypes.TenantId, "http://schemas.microsoft.com/identity/claims/tenantid");
    public string? FirstName => GetOptionalStringClaim(EntraClaimTypes.FirstName, ClaimTypes.GivenName);
    public string? LastName => GetOptionalStringClaim(EntraClaimTypes.LastName, ClaimTypes.Surname);
    public string DisplayName => GetOptionalStringClaim(EntraClaimTypes.DisplayName, ClaimTypes.Name,
            "preferred_username",
            "upn",
            "email",
            ClaimTypes.Email)
        ?? "Unknown user";

    public IReadOnlyCollection<ApplicationRole> Roles => GetApplicationRoles();
    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;
    private IReadOnlyCollection<ApplicationRole>GetApplicationRoles()
    {
        if (Principal is null)
        {
            return [];
        }

        var roleValues = Principal.Claims
            .Where(IsRoleClaim)
            .Select(claim => claim.Value)
            .Where(roleValue =>
                !string.IsNullOrWhiteSpace(roleValue))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roleValues.Length == 0)
        {
            return [];
        }

        var roles = new List<ApplicationRole>();

        foreach (var roleValue in roleValues)
        {
            if (!Enum.TryParse<ApplicationRole>(
                roleValue,
                ignoreCase: true,
                out var role))
            {
                throw new CurrentUserClaimException(
                    $"The '{roleValue}' application role is not supported by AssetManagement.");
            }

            if (!Enum.IsDefined(role) ||
                role == ApplicationRole.Unknown)
            {
                throw new CurrentUserClaimException($"The '{roleValue}' application role is not valid.");
            }

            roles.Add(role);
        }

        return roles.Distinct().ToArray();
    }

    private static bool IsRoleClaim(Claim claim)
    {
        return string.Equals(
                   claim.Type,
                   EntraClaimTypes.Role,
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   claim.Type,
                   ClaimTypes.Role,
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   claim.Type,
                   "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                   StringComparison.OrdinalIgnoreCase);
    }

    private Guid GetRequiredGuidClaim(params string[] claimTypes)
    {
        var claimValue = GetOptionalStringClaim(claimTypes);

        if (!Guid.TryParse(claimValue, out var claimId))
        {
            throw new CurrentUserClaimException(
                "The authenticated identity does not " +
                "contain a valid required identifier. " +
                $"Checked claim types: " +
                $"{string.Join(", ", claimTypes)}.");
        }

        return claimId;
    }

    private string? GetOptionalStringClaim(params string[] claimTypes)
    {
        if (Principal is null)
        {
            return null;
        }

        foreach (var claimType in claimTypes.Distinct(
                     StringComparer.OrdinalIgnoreCase))
        {
            var claimValue = Principal.FindFirstValue(claimType);

            if (!string.IsNullOrWhiteSpace(claimValue))
            {
                return claimValue.Trim();
            }
        }

        return null;
    }
}