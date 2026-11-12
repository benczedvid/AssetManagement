using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.ServiceDesk.GetServiceDeskSyncOperations;
using AssetManagement.Application.ServiceDesk.ReconcileServiceDeskRequest;
using AssetManagement.Application.ServiceDesk.Reconciliation;
using AssetManagement.Application.ServiceDesk.RequestServiceDeskNoteRetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace AssetManagement.WebApi.Controllers;

/// <summary>
/// Provides API endpoints for ServiceDesk Plus integration management.
/// </summary>
/// <remarks>
/// ServiceDesk request creation and note synchronization
/// are handled automatically by background processing.
///
/// This controller exposes administrative reconciliation
/// and audit operations only.
///
/// No endpoint in this controller creates an external
/// ServiceDesk request or task.
/// </remarks>
[ApiController]
[Route("api/service-desk")]
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public sealed class ServiceDeskController : ControllerBase
{
    /// <summary>
    /// Reconciles an uncertain ServiceDesk Plus request creation operation.
    /// </summary>
    /// <remarks>
    /// Associates an existing external request with a service case,
    /// or records that the external request was not created.
    ///
    /// The external result must be independently verified.
    ///
    /// This operation never creates a new ServiceDesk request.
    /// </remarks>
    /// <param name="id">
    /// The identifier of the associated service case.
    /// </param>
    /// <param name="request">
    /// The verified reconciliation result.
    /// </param>
    /// <param name="handler">
    /// The ServiceDesk request reconciliation handler.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// The updated request synchronization state.
    /// </returns>
    [HttpPost("requests/{id:guid}/reconcile")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<ReconcileServiceDeskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReconcileServiceDeskResponse>>ReconcileServiceDeskRequest(
            [FromRoute] Guid id,
            [FromBody] ReconcileServiceDeskRequest request,
            [FromServices] ReconcileServiceDeskRequestHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid service case identifier.",
                Detail = "The service case identifier cannot be empty.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (request.ServiceCaseId != id)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Service case identifier mismatch.",
                Detail = "The service case identifier in the request body must match the route identifier.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await handler.HandleAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves ServiceDesk synchronization operations.
    /// </summary>
    /// <remarks>
    /// Returns synchronization operations from:
    /// - ServiceDesk request creation,
    /// - ServiceDesk note creation,
    /// - ServiceDesk request closure.
    ///
    /// Results can be filtered by status, operation type,
    /// and service case identifier.
    ///
    /// Only users authorized to manage all service cases
    /// can access this endpoint.
    ///
    /// This operation is read-only.
    /// </remarks>
    /// <param name="request">
    /// Optional synchronization operation filters.
    /// </param>
    /// <param name="handler">
    /// The synchronization operation query handler.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// The filtered ServiceDesk synchronization operations.
    /// </returns>
    /// <response code="200">
    /// Synchronization operations were retrieved successfully.
    /// </response>
    /// <response code="400">
    /// One or more query parameters are invalid.
    /// </response>
    /// <response code="401">
    /// Authentication is required.
    /// </response>
    /// <response code="403">
    /// The current user does not have permission to manage all service cases.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred.
    /// </response>
    [HttpGet("operations")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Produces("application/json")]
    [ProducesResponseType<GetServiceDeskSyncOperationsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetServiceDeskSyncOperationsResponse>>GetServiceDeskSyncOperations(
            [FromQuery] GetServiceDeskSyncOperationsRequest request,
            [FromServices] GetServiceDeskSyncOperationsHandler handler,
            CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Resolves an uncertain ServiceDesk outbox operation.
    /// </summary>
    /// <remarks>
    /// Supports request creation, note creation, and request closure.
    ///
    /// The external result must be independently verified
    /// before this endpoint is called.
    ///
    /// This operation records the verified result and its audit entry.
    /// It does not create notes or requests, close requests,
    /// or automatically retry external operations.
    /// </remarks>
    [HttpPost("operations/reconcile")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<ResolveServiceDeskReconciliationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResolveServiceDeskReconciliationResponse>>ResolveServiceDeskReconciliation(
            [FromBody] ResolveServiceDeskReconciliationRequest request,
            [FromServices] ResolveServiceDeskReconciliationHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await handler.HandleAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Requests a retry for a manually reconciled failed note.
    /// </summary>
    /// <remarks>
    /// Requires a failed manual reconciliation audit matching
    /// the supplied processing operation identifier.
    ///
    /// The state change and retry audit are persisted atomically.
    ///
    /// This endpoint does not call the external ServiceDesk API.
    /// The background processor handles the pending note.
    /// </remarks>
    [HttpPost("notes/retry")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<RequestServiceDeskNoteRetryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RequestServiceDeskNoteRetryResponse>>RequestServiceDeskNoteRetry(
            [FromBody] RequestServiceDeskNoteRetryRequest request,
            [FromServices] RequestServiceDeskNoteRetryHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await handler.HandleAsync(request, cancellationToken);

        return Ok(response);
    }
}