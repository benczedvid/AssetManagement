using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AssetManagement.WebApi.IntegrationTests.Authentication
{
    /// <summary>
    /// Provides a deterministic authentication scheme for Web API
    /// integration tests.
    /// </summary>
    internal sealed class TestAuthenticationHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string AuthenticationScheme = "Test";
        public const string AuthenticationHeader = "X-Test-Authenticated";
        public const string EntraObjectIdHeader = "X-Test-Entra-Object-Id";
        public const string EntraTenantIdHeader = "X-Test-Entra-Tenant-Id";
        public const string ApplicationRoleHeader = "X-Test-Application-Role";
        public const string OmitLastNameHeader = "X-Test-Omit-Last-Name";
        private const string DefaultApplicationRole = "ApplicationAdministrator";

        private static readonly Guid DefaultEntraObjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid DefaultEntraTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!IsAuthenticatedRequest())
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var entraObjectId = GetGuidHeaderOrDefault(
                EntraObjectIdHeader,
                DefaultEntraObjectId);

            var entraTenantId = GetGuidHeaderOrDefault(
                EntraTenantIdHeader,
                DefaultEntraTenantId);

            var applicationRole = GetApplicationRole();

            var claims = new List<Claim>
            {
                new Claim("oid", entraObjectId.ToString()),
                new Claim("tid", entraTenantId.ToString()),
                new Claim("given_name", "John"),
                new Claim("name", "John Doe"),
                new Claim("preferred_username", "john.doe@praktiker.hu"),
                new Claim("scp", "access_as_user"),
                new Claim("roles", applicationRole)
            };

            if (!ShouldOmitLastName())
            {
                claims.Add(
                    new Claim("family_name", "Doe"));
            }

            var identity = new ClaimsIdentity(
                claims,
                AuthenticationScheme,
                "name",
                "roles");

            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(
                principal,
                AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        private bool IsAuthenticatedRequest()
        {
            return Request.Headers.TryGetValue(
                       AuthenticationHeader,
                       out var authenticatedValue)
                   &&
                   string.Equals(
                       authenticatedValue.ToString(),
                       "true",
                       StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldOmitLastName()
        {
            return Request.Headers.TryGetValue(
                       OmitLastNameHeader,
                       out var omitLastNameValue)
                   &&
                   string.Equals(
                       omitLastNameValue.ToString(),
                       "true",
                       StringComparison.OrdinalIgnoreCase);
        }

        private string GetApplicationRole()
        {
            if (Request.Headers.TryGetValue(
                    ApplicationRoleHeader,
                    out var roleValue))
            {
                var role = roleValue.ToString();

                if (!string.IsNullOrWhiteSpace(role))
                {
                    return role;
                }
            }

            return DefaultApplicationRole;
        }

        private Guid GetGuidHeaderOrDefault(
            string headerName,
            Guid defaultValue)
        {
            if (Request.Headers.TryGetValue(
                    headerName,
                    out var headerValue)
                &&
                Guid.TryParse(
                    headerValue.ToString(),
                    out var parsedValue))
            {
                return parsedValue;
            }

            return defaultValue;
        }
    }
}