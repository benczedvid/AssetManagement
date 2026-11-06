using AssetManagement.Application.AssetMovements.CreateAssetMovement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.WebApi.Controllers
{
    [Route("api/asset-movement")]
    [ApiController]
    public class AssetMovementController : ControllerBase
    {
        private readonly CreateAssetMovementHandler _handler;

        public AssetMovementController(CreateAssetMovementHandler handler)
        {
            _handler = handler;
        }
        /// <summary>
        /// Creates a new asset movement.
        /// </summary>
        /// <remarks>
        /// Creates a new asset movement based on the provided request data.
        /// The movement records the transfer or change of location of an asset.
        /// </remarks>
        /// <param name="request">The asset movement data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The newly created asset movement.</returns>
        /// <response code="201">The asset movement was created successfully.</response>
        /// <response code="400">The request contains invalid data.</response>
        /// <response code="404">The specified asset or related entity was not found.</response>
        /// <response code="409">The asset movement conflicts with the current state of the asset.</response>
        /// <response code="500">An unexpected error occurred while creating the asset movement.</response>
        [HttpPost]
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(CreateAssetMovementResponse), StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreateAssetMovementResponse>> CreateAssetMovement([FromBody] CreateAssetMovementRequest request, CancellationToken cancellationToken)
        {
            var response = await _handler.HandleAsync(request, cancellationToken);
            return Created($"/api/asset-movement/{response.Id}", response);
        }
    }
}
