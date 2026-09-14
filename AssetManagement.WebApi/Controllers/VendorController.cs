using AssetManagement.Application.Vendors.CreateVendor;
using AssetManagement.Application.Vendors.GetVendorById;
using AssetManagement.Application.Vendors.GetVendors;
using AssetManagement.Application.Vendors.UpdateVendor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace AssetManagement.WebApi.Controllers
{
    [Route("api/vendors")]
    [ApiController]
    [Authorize]
    public class VendorController : ControllerBase
    {
        private readonly CreateVendorHandler _createVendorHandler;
        private readonly GetVendorByIdHandler _getVendorByIdHandler;
        private readonly GetVendorsHandler _getVendorsHandler;
        private readonly UpdateVendorHandler _updateVendorHandler;

        public VendorController(
            CreateVendorHandler createVendorHandler,
            GetVendorByIdHandler getVendorByIdHandler,
            GetVendorsHandler getVendorsHandler,
            UpdateVendorHandler updateVendorHandler)
        {
            ArgumentNullException.ThrowIfNull(createVendorHandler);
            ArgumentNullException.ThrowIfNull(getVendorByIdHandler);
            ArgumentNullException.ThrowIfNull(getVendorsHandler);
            ArgumentNullException.ThrowIfNull(updateVendorHandler);

            _createVendorHandler = createVendorHandler;
            _getVendorByIdHandler = getVendorByIdHandler;
            _getVendorsHandler = getVendorsHandler;
            _updateVendorHandler = updateVendorHandler;
        }

        [HttpPost]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType<CreateVendorResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreateVendorResponse>> CreateVendor([FromBody] CreateVendorRequest request, CancellationToken token)
        {
            var vendor = await _createVendorHandler.HandleAsync(request, token);

            return CreatedAtAction(nameof(GetVendorById), new { id = vendor.Id }, vendor);
        }

        [HttpGet("{id:guid}")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<GetVendorByIdResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetVendorByIdResponse>> GetVendorById([FromRoute] Guid id, CancellationToken token)
        {
            var vendor = await _getVendorByIdHandler.HandleAsync(id, token);

            return Ok(vendor);
        }

        [HttpGet]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<IReadOnlyList<GetVendorsResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<GetVendorsResponse>>> GetVendors(CancellationToken token)
        {
            var vendors = await _getVendorsHandler.HandleAsync(token);

            return Ok(vendors);
        }

        [HttpPut("{id:guid}")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Produces("application/json")]
        [ProducesResponseType<IReadOnlyList<UpdateVendorResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UpdateVendorResponse>> UpdateVendor([FromRoute] Guid id, [FromBody] UpdateVendorRequest request, CancellationToken token)
        {
            var vendor = await _updateVendorHandler.HandleAsync(id, request, token);

            return Ok(vendor);
        }
    }
}
