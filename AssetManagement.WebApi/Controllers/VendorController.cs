using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Vendors.AttachContactToVendor;
using AssetManagement.Application.Vendors.CreateVendor;
using AssetManagement.Application.Vendors.DetachContactFromVendor;
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
        private readonly AttachContactToVendorHandler _attachContactToVendorHandler;
        private readonly DetachContactFromVendorHandler _detachContactFromVendorHandler;

        public VendorController(
            CreateVendorHandler createVendorHandler,
            GetVendorByIdHandler getVendorByIdHandler,
            GetVendorsHandler getVendorsHandler,
            UpdateVendorHandler updateVendorHandler,
            AttachContactToVendorHandler attachContactToVendorHandler,
            DetachContactFromVendorHandler detachContactFromVendorHandler)
        {
            ArgumentNullException.ThrowIfNull(createVendorHandler);
            ArgumentNullException.ThrowIfNull(getVendorByIdHandler);
            ArgumentNullException.ThrowIfNull(getVendorsHandler);
            ArgumentNullException.ThrowIfNull(updateVendorHandler);
            ArgumentNullException.ThrowIfNull(attachContactToVendorHandler);
            ArgumentNullException.ThrowIfNull(detachContactFromVendorHandler);

            _createVendorHandler = createVendorHandler;
            _getVendorByIdHandler = getVendorByIdHandler;
            _getVendorsHandler = getVendorsHandler;
            _updateVendorHandler = updateVendorHandler;
            _attachContactToVendorHandler = attachContactToVendorHandler;
            _detachContactFromVendorHandler = detachContactFromVendorHandler;
        }

        /// <summary>
        /// Creates a new vendor.
        /// </summary>
        /// <remarks>
        /// Creates a new vendor using the provided vendor information.
        ///
        /// Before creating the vendor, the operation verifies that no existing
        /// vendor is registered with the specified name.
        ///
        /// The vendor information includes:
        /// - the vendor name,
        /// - the complete address,
        /// - an optional webpage.
        ///
        /// The authenticated user must have the ManageVendors permission and the
        /// required API scope.
        /// </remarks>
        /// <param name="request">
        /// The data required to create the vendor, including the name,
        /// address, and optional webpage.
        /// </param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The newly created vendor.</returns>
        /// <response code="201">The vendor was created successfully.</response>
        /// <response code="400">The request contains invalid vendor or address data.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="409">A vendor with the specified name already exists.</response>
        /// <response code="500">An unexpected error occurred while creating the vendor.</response>
        [HttpPost]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
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

        /// <summary>
        /// Retrieves a vendor by its unique identifier.
        /// </summary>
        /// <remarks>
        /// Returns detailed information about the vendor identified by the specified ID.
        ///
        /// The response includes:
        /// - the vendor name,
        /// - the complete address,
        /// - the vendor webpage, when available,
        /// - the registered vendor contacts.
        ///
        /// Each contact includes the contact person's name, job title,
        /// phone number, and email address when available.
        ///
        /// The authenticated user must have the ManageVendors permission and the
        /// required API scope.
        /// </remarks>
        /// <param name="id">The unique identifier of the vendor.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The vendor matching the specified identifier.</returns>
        /// <response code="200">The vendor was retrieved successfully.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="404">The specified vendor was not found.</response>
        /// <response code="500">An unexpected error occurred while retrieving the vendor.</response>
        [HttpGet("{id:guid}")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
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
        /// <summary>
        /// Retrieves all vendors.
        /// </summary>
        /// <remarks>
        /// Returns all vendors registered in the system.
        ///
        /// Each returned vendor contains:
        /// - the unique identifier,
        /// - the vendor name,
        /// - the complete address,
        /// - the vendor webpage, when available.
        ///
        /// The authenticated user must have the ManageVendors permission and the
        /// required API scope.
        /// </remarks>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A list of all vendors registered in the system.</returns>
        /// <response code="200">The vendors were retrieved successfully.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="500">An unexpected error occurred while retrieving the vendors.</response>
        [HttpGet]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
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

        /// <summary>
        /// Updates an existing vendor.
        /// </summary>
        /// <remarks>
        /// Updates the information of the specified vendor.
        ///
        /// The operation updates:
        /// - the vendor name,
        /// - the complete address,
        /// - the vendor webpage.
        ///
        /// The operation can be performed only if the specified vendor exists.
        ///
        /// Contacts are not changed by this operation; use the vendor contact
        /// endpoints to attach or detach contacts.
        ///
        /// The authenticated user must have the ManageVendors permission and the
        /// required API scope.
        /// </remarks>
        /// <param name="id">The unique identifier of the vendor to update.</param>
        /// <param name="request">
        /// The updated vendor data, including the name, address and webpage.
        /// </param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The updated vendor information.</returns>
        /// <response code="200">The vendor was updated successfully.</response>
        /// <response code="400">The request contains invalid vendor or address data.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="404">The specified vendor was not found.</response>
        /// <response code="500">An unexpected error occurred while updating the vendor.</response>
        [HttpPut("{id:guid}")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
        [Produces("application/json")]
        [ProducesResponseType<UpdateVendorResponse>(StatusCodes.Status200OK)]
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

        /// <summary>
        /// Attaches an existing standalone contact to a vendor.
        /// </summary>
        /// <remarks>
        /// A contact can be attached to at most one vendor. Attaching a contact that is
        /// already attached to the same vendor has no effect.
        /// </remarks>
        /// <param name="id">The unique identifier of the vendor.</param>
        /// <param name="contactId">The unique identifier of the contact.</param>
        /// <param name="token">Cancellation token.</param>
        /// <response code="204">The contact was attached successfully.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="404">The specified vendor or contact was not found.</response>
        /// <response code="409">The contact is attached to another vendor, or its phone number is already used by a contact of this vendor.</response>
        /// <response code="500">An unexpected error occurred while attaching the contact.</response>
        [HttpPut("{id:guid}/contacts/{contactId:guid}")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AttachContact([FromRoute] Guid id, [FromRoute] Guid contactId, CancellationToken token)
        {
            await _attachContactToVendorHandler.HandleAsync(id, contactId, token);

            return NoContent();
        }

        /// <summary>
        /// Detaches a contact from a vendor.
        /// </summary>
        /// <remarks>
        /// The contact is not deleted; it remains available as a standalone contact.
        /// </remarks>
        /// <param name="id">The unique identifier of the vendor.</param>
        /// <param name="contactId">The unique identifier of the contact.</param>
        /// <param name="token">Cancellation token.</param>
        /// <response code="204">The contact was detached successfully.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="404">The specified vendor was not found.</response>
        /// <response code="409">The contact is not attached to this vendor.</response>
        /// <response code="500">An unexpected error occurred while detaching the contact.</response>
        [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DetachContact([FromRoute] Guid id, [FromRoute] Guid contactId, CancellationToken token)
        {
            await _detachContactFromVendorHandler.HandleAsync(id, contactId, token);

            return NoContent();
        }
    }
}
