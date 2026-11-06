using AssetManagement.Application.Users.GetApplicationUsers;
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
        private readonly GetApplicationUsersHandler _getApplicationUsersHandler;

        public UsersController(
            GetOrCreateCurrentUserHandler handler,
            GetApplicationUsersHandler getApplicationUsersHandler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            ArgumentNullException.ThrowIfNull(getApplicationUsersHandler);

            _handler = handler;
            _getApplicationUsersHandler = getApplicationUsersHandler;
        }
        
        /// <summary>
        /// Retrieves the current authenticated application user.
        /// </summary>
        /// <remarks>
        /// Retrieves and synchronizes the application user associated with the
        /// currently authenticated Microsoft Entra ID user.
        ///
        /// If the application user does not yet exist, a new user record is created.
        /// If the user already exists, the stored profile and access information are
        /// synchronized with the current Microsoft Entra ID profile.
        ///
        /// The operation validates that:
        /// - the user is authenticated,
        /// - the Entra Object ID and Tenant ID are valid,
        /// - the user exists in Microsoft Entra ID,
        /// - the Entra ID profile belongs to the authenticated user,
        /// - the user has a valid display name,
        /// - exactly one valid application role is assigned.
        ///
        /// For store-scoped roles, the employee number is used to resolve the
        /// employee and the corresponding store assignment.
        ///
        /// The user's login is registered after successful synchronization.
        ///
        /// The request requires the configured API scope.
        /// </remarks>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The synchronized current application user.</returns>
        /// <response code="200">The current user was retrieved and synchronized successfully.</response>
        /// <response code="401">The user could not be authenticated or does not have valid identity, role, or account information.</response>
        /// <response code="403">The authenticated user does not have the required API scope.</response>
        /// <response code="500">An unexpected error occurred while retrieving or synchronizing the current user.</response>
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

        /// <summary>
        /// Retrieves all application users.
        /// </summary>
        /// <remarks>
        /// Returns all application users registered in the system.
        ///
        /// Each returned user contains the unique identifier and display
        /// name required for user selection.
        ///
        /// The returned users can be used when assigning a courier to a
        /// package.
        ///
        /// The authenticated user must have the required API scope.
        /// </remarks>
        /// <param name="token">Cancellation token.</param>
        /// <returns>
        /// A list of all application users registered in the system.
        /// </returns>
        /// <response code="200">
        /// The application users were retrieved successfully.
        /// </response>
        /// <response code="401">
        /// Authentication is required to perform this operation.
        /// </response>
        /// <response code="403">
        /// The authenticated user does not have the required API scope.
        /// </response>
        /// <response code="500">
        /// An unexpected error occurred while retrieving the application
        /// users.
        /// </response>
        [HttpGet]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<IReadOnlyList<GetApplicationUsersResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<GetApplicationUsersResponse>>>
            GetApplicationUsers(CancellationToken token)
        {
            var users = await _getApplicationUsersHandler.HandleAsync(token);

            return Ok(users);
        }
    }
}
