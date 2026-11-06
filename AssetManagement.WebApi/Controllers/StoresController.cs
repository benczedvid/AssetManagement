using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Stores.CreateStore;
using AssetManagement.Application.Stores.GetStoreById;
using AssetManagement.Application.Stores.GetStores;
using AssetManagement.Application.Stores.UpdateStore;
using AssetManagement.Application.Assets.GetAsset;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace AssetManagement.WebApi.Controllers
{
    [Route("api/stores")]
    [ApiController]
    [Authorize]
    public class StoresController : ControllerBase
    {
        private readonly CreateStoreHandler _createStoreHandler;
        private readonly GetStoreByIdHandler _getStoreByIdHandler;
        private readonly GetStoresHandler _getStoresHandler;
        private readonly UpdateStoreHandler _updateStoreHandler;

        public StoresController(
            CreateStoreHandler createStoreHandler,
            GetStoreByIdHandler getStoreByIdHandler,
            GetStoresHandler getStoresHandler,
            UpdateStoreHandler updateStoreHandler)
        {
            ArgumentNullException.ThrowIfNull(createStoreHandler);
            ArgumentNullException.ThrowIfNull(getStoreByIdHandler);
            ArgumentNullException.ThrowIfNull(getStoresHandler);
            ArgumentNullException.ThrowIfNull(updateStoreHandler);
            _createStoreHandler = createStoreHandler;
            _getStoreByIdHandler = getStoreByIdHandler;
            _getStoresHandler = getStoresHandler;
            _updateStoreHandler = updateStoreHandler;
        }

        /// <summary>
        /// Creates a new store.
        /// </summary>
        /// <remarks>
        /// Creates a new store using the provided store and address information.
        ///
        /// Before creating the store, the operation verifies that the specified
        /// store number is not already assigned to an existing store.
        ///
        /// The authenticated user must have the ManageStores permission and the
        /// required API scope.
        /// </remarks>
        /// <param name="request">
        /// The data required to create the store, including the store number,
        /// name, and address.
        /// </param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The newly created store.</returns>
        /// <response code="201">The store was created successfully.</response>
        /// <response code="400">The request contains invalid store or address data.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage stores.</response>
        /// <response code="409">A store with the specified store number already exists.</response>
        /// <response code="500">An unexpected error occurred while creating the store.</response>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManageStores)]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType<CreateStoreResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreateStoreResponse>> CreateStore([FromBody]CreateStoreRequest request, CancellationToken token)
        {
            var store = await _createStoreHandler.HandleAsync(request, token);

            return CreatedAtAction(nameof(GetStoreById), new { id = store.Id}, store);
        }

        /// <summary>
        /// Retrieves a store by its unique identifier.
        /// </summary>
        /// <remarks>
        /// Returns the store identified by the specified ID.
        ///
        /// The response includes the store number, name, and complete address
        /// information.
        ///
        /// The authenticated user must have the ManageStores permission and the
        /// required API scope.
        /// </remarks>
        /// <param name="id">The unique identifier of the store.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The store matching the specified identifier.</returns>
        /// <response code="200">The store was retrieved successfully.</response>
        /// <response code="400">The store identifier is invalid.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage stores.</response>
        /// <response code="404">The store was not found.</response>
        /// <response code="500">An unexpected error occurred while retrieving the store.</response>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.ManageStores)]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<GetStoreByIdResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetStoreByIdResponse>>GetStoreById([FromRoute] Guid id, CancellationToken token)
        {
            var store = await _getStoreByIdHandler.HandleAsync(id, token);

            return Ok(store);
        }

        /// <summary>
        /// Retrieves all stores.
        /// </summary>
        /// <remarks>
        /// Returns all stores registered in the system.
        ///
        /// Each returned store contains its unique identifier, store number,
        /// name, and complete address information.
        ///
        /// The authenticated user must have the ManageStores permission and the
        /// required API scope.
        /// </remarks>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A list of all stores registered in the system.</returns>
        /// <response code="200">The stores were retrieved successfully.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage stores.</response>
        /// <response code="500">An unexpected error occurred while retrieving the stores.</response>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManageStores)]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<IReadOnlyList<GetStoresResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<GetStoresResponse>>>GetStores(CancellationToken token)
        {
            var stores = await _getStoresHandler.HandleAsync(token);

            return Ok(stores);
        }

        /// <summary>
        /// Updates an existing store.
        /// </summary>
        /// <remarks>
        /// Updates the name and address information of the specified store.
        ///
        /// The store number cannot be changed by this operation.
        ///
        /// The operation can be performed only if the specified store exists.
        /// The updated address includes the country code, postal code, city,
        /// public space name and type, and house number.
        ///
        /// The authenticated user must have the ManageStores permission and the
        /// required API scope.
        /// </remarks>
        /// <param name="id">The unique identifier of the store to update.</param>
        /// <param name="request">
        /// The updated store data, including the store name and address.
        /// </param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The updated store information.</returns>
        /// <response code="200">The store was updated successfully.</response>
        /// <response code="400">The store identifier or request data is invalid.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage stores.</response>
        /// <response code="404">The specified store was not found.</response>
        /// <response code="500">An unexpected error occurred while updating the store.</response>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.ManageStores)]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<UpdateStoreRequest>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UpdateStoreResponse>> UpdateStore([FromRoute] Guid id,[FromBody] UpdateStoreRequest request, CancellationToken token)
        {
            var stores = await _updateStoreHandler.HandleAsync(id, request, token);

            return Ok(stores);
        }
    }
}
