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
