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
    internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string AuthenticationScheme = "Test";
        public const string AuthenticationHeader = "X-Test-Authenticated";
        public const string EntraObjectIdHeader = "X-Test-Entra-Object-Id";
        public const string EntraTenantIdHeader = "X-Test-Entra-Tenant-Id";
        public const string OmitLastNameHeader = "X-Test-Omit-Last-Name";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(
                AuthenticationHeader,
                out var authenticatedValue) ||
                !string.Equals(
                    authenticatedValue.ToString(),
                    "true",
                    StringComparison.OrdinalIgnoreCase)) {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
            var entraObjectId = GetGuidHeaderOrDefault(EntraObjectIdHeader);
            var entraTenantId = GetGuidHeaderOrDefault(EntraTenantIdHeader);

            var claims = new List<Claim> {
                new ("oid", entraObjectId.ToString()),
                new ("tid", entraTenantId.ToString()),
                new ("given_name", "Luke"),
                new ("name", "Luke Skywalker"),
                new ("scp", "access_as_user")
            };

            var shouldOmitLastName = Request.Headers.TryGetValue(
                OmitLastNameHeader,
                out var omitLastNameValue) &&
                string.Equals(
                omitLastNameValue.ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase);

            if (!shouldOmitLastName)
            {
                claims.Add(
                    new Claim(
                        "family_name",
                        "Skywalker"));
            }
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));

            
        }
        private Guid GetGuidHeaderOrDefault(string headerName)
        {
            if (Request.Headers.TryGetValue(
                    headerName,
                    out var headerValue) &&
                Guid.TryParse(
                    headerValue.ToString(),
                    out var parsedValue))
            {
                return parsedValue;
            }

            return Guid.NewGuid();
        }
    }
}
