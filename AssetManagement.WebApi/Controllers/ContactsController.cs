using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Contacts;
using AssetManagement.Application.Contacts.CreateContact;
using AssetManagement.Application.Contacts.DeleteContact;
using AssetManagement.Application.Contacts.GetContactById;
using AssetManagement.Application.Contacts.GetContacts;
using AssetManagement.Application.Contacts.UpdateContact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace AssetManagement.WebApi.Controllers
{
    [Route("api/contacts")]
    [ApiController]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly CreateContactHandler _createContactHandler;
        private readonly GetContactByIdHandler _getContactByIdHandler;
        private readonly GetContactsHandler _getContactsHandler;
        private readonly UpdateContactHandler _updateContactHandler;
        private readonly DeleteContactHandler _deleteContactHandler;

        public ContactsController(
            CreateContactHandler createContactHandler,
            GetContactByIdHandler getContactByIdHandler,
            GetContactsHandler getContactsHandler,
            UpdateContactHandler updateContactHandler,
            DeleteContactHandler deleteContactHandler)
        {
            ArgumentNullException.ThrowIfNull(createContactHandler);
            ArgumentNullException.ThrowIfNull(getContactByIdHandler);
            ArgumentNullException.ThrowIfNull(getContactsHandler);
            ArgumentNullException.ThrowIfNull(updateContactHandler);
            ArgumentNullException.ThrowIfNull(deleteContactHandler);

            _createContactHandler = createContactHandler;
            _getContactByIdHandler = getContactByIdHandler;
            _getContactsHandler = getContactsHandler;
            _updateContactHandler = updateContactHandler;
            _deleteContactHandler = deleteContactHandler;
        }

        /// <summary>
        /// Creates a new standalone contact.
        /// </summary>
        /// <remarks>
        /// The created contact does not belong to any vendor. It can be attached to a vendor
        /// later with the <c>PUT api/vendors/{vendorId}/contacts/{contactId}</c> endpoint.
        /// </remarks>
        /// <param name="request">The contact data.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The newly created contact.</returns>
        /// <response code="201">The contact was created successfully.</response>
        /// <response code="400">The request contains invalid contact data.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="500">An unexpected error occurred while creating the contact.</response>
        [HttpPost]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType<ContactDetailsResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ContactDetailsResponse>> CreateContact([FromBody] CreateContactRequest request, CancellationToken token)
        {
            var contact = await _createContactHandler.HandleAsync(request, token);

            return CreatedAtAction(nameof(GetContactById), new { id = contact.Id }, contact);
        }

        /// <summary>
        /// Retrieves a contact by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the contact.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The contact matching the specified identifier.</returns>
        /// <response code="200">The contact was retrieved successfully.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="404">The specified contact was not found.</response>
        /// <response code="500">An unexpected error occurred while retrieving the contact.</response>
        [HttpGet("{id:guid}")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
        [Produces("application/json")]
        [ProducesResponseType<ContactDetailsResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ContactDetailsResponse>> GetContactById([FromRoute] Guid id, CancellationToken token)
        {
            var contact = await _getContactByIdHandler.HandleAsync(id, token);

            return Ok(contact);
        }

        /// <summary>
        /// Retrieves all contacts, both attached and not attached to a vendor.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>All contacts registered in the system.</returns>
        /// <response code="200">The contacts were retrieved successfully.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="500">An unexpected error occurred while retrieving the contacts.</response>
        [HttpGet]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
        [Produces("application/json")]
        [ProducesResponseType<IReadOnlyList<ContactDetailsResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<ContactDetailsResponse>>> GetContacts(CancellationToken token)
        {
            var contacts = await _getContactsHandler.HandleAsync(token);

            return Ok(contacts);
        }

        /// <summary>
        /// Updates an existing contact.
        /// </summary>
        /// <remarks>
        /// When the contact is attached to a vendor, its phone number must stay unique among
        /// the contacts of that vendor.
        /// </remarks>
        /// <param name="id">The unique identifier of the contact.</param>
        /// <param name="request">The updated contact data.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The updated contact.</returns>
        /// <response code="200">The contact was updated successfully.</response>
        /// <response code="400">The request contains invalid contact data.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="404">The specified contact was not found.</response>
        /// <response code="409">Another contact of the same vendor already has this phone number.</response>
        /// <response code="500">An unexpected error occurred while updating the contact.</response>
        [HttpPut("{id:guid}")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType<ContactDetailsResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ContactDetailsResponse>> UpdateContact([FromRoute] Guid id, [FromBody] UpdateContactRequest request, CancellationToken token)
        {
            var contact = await _updateContactHandler.HandleAsync(id, request, token);

            return Ok(contact);
        }

        /// <summary>
        /// Deletes a contact.
        /// </summary>
        /// <remarks>
        /// If the contact is attached to a vendor, it is removed from that vendor as well.
        /// </remarks>
        /// <param name="id">The unique identifier of the contact.</param>
        /// <param name="token">Cancellation token.</param>
        /// <response code="204">The contact was deleted successfully.</response>
        /// <response code="401">Authentication is required to perform this operation.</response>
        /// <response code="403">The user does not have permission or the required scope to manage vendors.</response>
        /// <response code="404">The specified contact was not found.</response>
        /// <response code="500">An unexpected error occurred while deleting the contact.</response>
        [HttpDelete("{id:guid}")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        [Authorize(Policy = AuthorizationPolicies.ManageVendors)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteContact([FromRoute] Guid id, CancellationToken token)
        {
            await _deleteContactHandler.HandleAsync(id, token);

            return NoContent();
        }
    }
}
