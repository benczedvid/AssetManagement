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

        [HttpGet("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.ManageStores)]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<GetAssetResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetAssetResponse>>GetStoreById([FromRoute] Guid id, CancellationToken token)
        {
            var store = await _getStoreByIdHandler.HandleAsync(id, token);

            return Ok(store);
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManageStores)]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<IReadOnlyList<GetAssetResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<GetStoresResponse>>>GetStores(CancellationToken token)
        {
            var stores = await _getStoresHandler.HandleAsync(token);

            return Ok(stores);
        }
        [HttpPut("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.ManageStores)]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<IReadOnlyList<GetAssetResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UpdateStoreResponse>> UpdateStore([FromRoute] Guid id,[FromBody] UpdateStoreRequest request, CancellationToken token)
        {
            var stores = await _updateStoreHandler.HandleAsync(id, request, token);

            return Ok(stores);
        }
    }
}
