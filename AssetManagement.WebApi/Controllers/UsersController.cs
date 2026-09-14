using AssetManagement.Application.Users.GetOrCreateCurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using System.Security.Claims;

namespace AssetManagement.WebApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly GetOrCreateCurrentUserHandler _handler;

        public UsersController(GetOrCreateCurrentUserHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            _handler = handler;
        }

        [HttpGet("me")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CurrentUserResponse>>GetMe(CancellationToken cancellationToken)
        {
            var response = await _handler.HandleAsync(cancellationToken);

            return Ok(response);
        }
    }
}
