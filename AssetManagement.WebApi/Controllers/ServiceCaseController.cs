using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.ServiceCases.CloseServiceCase;
using AssetManagement.Application.ServiceCases.CompleteRepairAtHeadquarters;
using AssetManagement.Application.ServiceCases.ConfirmReceivedByExternalService;
using AssetManagement.Application.ServiceCases.DispatchToExternalService;
using AssetManagement.Application.ServiceCases.DisposeServiceCaseAsset;
using AssetManagement.Application.ServiceCases.GetServiceCaseDetails;
using AssetManagement.Application.ServiceCases.GetServiceCases;
using AssetManagement.Application.ServiceCases.HandOverToExternalService;
using AssetManagement.Application.ServiceCases.OpenServiceCase;
using AssetManagement.Application.ServiceCases.RequireExternalService;
using AssetManagement.Application.ServiceCases.ResolveAfterExternalServiceReturn;
using AssetManagement.Application.ServiceCases.ReturnFromExternalService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace AssetManagement.WebApi.Controllers;

[ApiController]
[Route("api/service-cases")]
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public sealed class ServiceCasesController : ControllerBase
{
    private readonly OpenServiceCaseHandler _openServiceCaseHandler;
    private readonly RequireExternalServiceHandler _requireExternalServiceHandler;
    private readonly DispatchToExternalServiceHandler _dispatchToExternalServiceHandler;
    private readonly HandOverToExternalServiceHandler _handOverToExternalServiceHandler;
    private readonly ConfirmReceivedByExternalServiceHandler _confirmReceivedByExternalServiceHandler;
    private readonly ReturnFromExternalServiceHandler _returnFromExternalServiceHandler;
    private readonly CompleteRepairAtHeadquartersHandler _completeRepairAtHeadquartersHandler;
    private readonly ResolveAfterExternalServiceReturnHandler _resolveAfterExternalServiceReturnHandler;
    private readonly CloseServiceCaseHandler _closeServiceCaseHandler;
    private readonly GetServiceCasesHandler _getServiceCasesHandler;
    private readonly GetServiceCaseDetailsHandler _getServiceCaseDetailsHandler;

    public ServiceCasesController(
        OpenServiceCaseHandler openServiceCaseHandler,
        RequireExternalServiceHandler requireExternalServiceHandler,
        DispatchToExternalServiceHandler dispatchToExternalServiceHandler,
        HandOverToExternalServiceHandler handOverToExternalServiceHandler,
        ConfirmReceivedByExternalServiceHandler confirmReceivedByExternalServiceHandler,
        ReturnFromExternalServiceHandler returnFromExternalServiceHandler,
        CompleteRepairAtHeadquartersHandler completeRepairAtHeadquartersHandler,
        ResolveAfterExternalServiceReturnHandler resolveAfterExternalServiceReturnHandler,
        CloseServiceCaseHandler closeServiceCaseHandler,
        GetServiceCasesHandler getServiceCasesHandler,
        GetServiceCaseDetailsHandler getServiceCaseDetailsHandler)
    {
        ArgumentNullException.ThrowIfNull(openServiceCaseHandler);
        ArgumentNullException.ThrowIfNull(requireExternalServiceHandler);
        ArgumentNullException.ThrowIfNull(dispatchToExternalServiceHandler);
        ArgumentNullException.ThrowIfNull(handOverToExternalServiceHandler);
        ArgumentNullException.ThrowIfNull(confirmReceivedByExternalServiceHandler);
        ArgumentNullException.ThrowIfNull(returnFromExternalServiceHandler);
        ArgumentNullException.ThrowIfNull(completeRepairAtHeadquartersHandler);
        ArgumentNullException.ThrowIfNull(resolveAfterExternalServiceReturnHandler);
        ArgumentNullException.ThrowIfNull(closeServiceCaseHandler);
        ArgumentNullException.ThrowIfNull(getServiceCasesHandler);
        ArgumentNullException.ThrowIfNull(getServiceCaseDetailsHandler);

        _openServiceCaseHandler = openServiceCaseHandler;
        _requireExternalServiceHandler = requireExternalServiceHandler;
        _dispatchToExternalServiceHandler = dispatchToExternalServiceHandler;
        _handOverToExternalServiceHandler = handOverToExternalServiceHandler;
        _confirmReceivedByExternalServiceHandler = confirmReceivedByExternalServiceHandler;
        _returnFromExternalServiceHandler = returnFromExternalServiceHandler;
        _completeRepairAtHeadquartersHandler = completeRepairAtHeadquartersHandler;
        _resolveAfterExternalServiceReturnHandler = resolveAfterExternalServiceReturnHandler;
        _closeServiceCaseHandler = closeServiceCaseHandler;
        _getServiceCasesHandler = getServiceCasesHandler;
        _getServiceCaseDetailsHandler = getServiceCaseDetailsHandler;
    }

    /// <summary>
    /// Opens a new service case for an asset.
    /// </summary>
    /// <remarks>
    /// Creates a new service case for the specified asset.
    ///
    /// A service case can be opened only if:
    /// - the asset exists,
    /// - the current user is authorized to manage service cases for the asset,
    /// - the asset is within the current user's store access scope,
    /// - the asset is not assigned to an employee,
    /// - the asset is currently either in a store or at headquarters,
    /// - the asset does not already have an active service case.
    ///
    /// If the asset is currently in a store, the service case is created with
    /// the AwaitingCentralReceipt status. The asset remains in the store until
    /// a related package is dispatched to headquarters.
    ///
    /// If the asset is already at headquarters, the service case is created
    /// with the UnderCentralInspection status.
    ///
    /// The operation records the creation of the service case as a service case
    /// event. Opening the service case does not change the asset status.
    /// The asset status changes only when the related package is dispatched.
    ///
    /// The authenticated user must have the ManageServiceCases permission.
    /// </remarks>
    /// <param name="request">The data required to open the service case.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created service case.</returns>
    /// <response code="201">The service case was created successfully.</response>
    /// <response code="400">The request contains an invalid asset identifier or invalid service case data.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to manage the service case or access the asset's store.</response>
    /// <response code="404">The specified asset was not found.</response>
    /// <response code="409">The asset is not in a valid state for opening a service case, is still assigned to an employee, or already has an active service case.</response>
    /// <response code="500">An unexpected error occurred while opening the service case.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<OpenServiceCaseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OpenServiceCaseResponse>>OpenServiceCase([FromBody] OpenServiceCaseRequest request, CancellationToken cancellationToken)
    {
        var response = await _openServiceCaseHandler.HandleAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Marks a service case as requiring external service.
    /// </summary>
    /// <remarks>
    /// Records the decision that the asset requires repair by an external
    /// service provider and assigns the selected vendor to the service case.
    ///
    /// The operation can be performed only if:
    /// - the service case exists,
    /// - the related asset exists,
    /// - the selected vendor exists,
    /// - the service case is in the UnderCentralInspection status,
    /// - the related asset is located at headquarters,
    /// - no vendor has already been assigned to the service case.
    ///
    /// An ExternalServiceRequired service case event is created with the
    /// optional comment provided in the request.
    ///
    /// The authenticated user must have permission to manage all service cases.
    /// </remarks>
    /// <param name="id">The unique identifier of the service case.</param>
    /// <param name="request">
    /// The external service decision data, including the vendor identifier
    /// and an optional comment.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The updated service case information, including the selected vendor
    /// and the user who made the decision.
    /// </returns>
    /// <response code="200">The external service requirement was recorded successfully.</response>
    /// <response code="400">The service case identifier, vendor identifier, or request data is invalid.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to manage all service cases.</response>
    /// <response code="404">The service case, related asset, or selected vendor was not found.</response>
    /// <response code="409">The service case or asset is not in a valid state for requiring external service, or a vendor has already been assigned.</response>
    /// <response code="500">An unexpected error occurred while updating the service case.</response>
    [HttpPost("{id:guid}/require-external-service")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<RequireExternalServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RequireExternalServiceResponse>>RequireExternalService(
            [FromRoute] Guid id,
            [FromBody] RequireExternalServiceRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _requireExternalServiceHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Dispatches an asset to the assigned external service provider.
    /// </summary>
    /// <remarks>
    /// Registers the dispatch of the asset from headquarters to the external
    /// service provider assigned to the service case.
    ///
    /// The operation can be performed only if:
    /// - the service case exists,
    /// - the related asset exists,
    /// - the service case is awaiting external service dispatch,
    /// - a valid vendor is assigned to the service case,
    /// - the related asset is located at headquarters.
    ///
    /// The operation updates both the service case and the related asset to
    /// reflect that the asset is in transit to the external service provider.
    ///
    /// A DispatchedToExternalService event is recorded for the service case
    /// with the optional comment provided in the request. The asset status
    /// change is also recorded in the asset history.
    ///
    /// The authenticated user must have permission to manage all service cases.
    /// </remarks>
    /// <param name="id">The unique identifier of the service case.</param>
    /// <param name="request">
    /// The dispatch data, including an optional comment.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The updated service case and asset status information, including the
    /// assigned vendor and dispatch details.
    /// </returns>
    /// <response code="200">The asset was successfully dispatched to the external service provider.</response>
    /// <response code="400">The service case identifier or request data is invalid.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to manage all service cases.</response>
    /// <response code="404">The service case or its related asset was not found.</response>
    /// <response code="409">The service case or asset is not in a valid state for dispatch to the external service provider.</response>
    /// <response code="500">An unexpected error occurred while dispatching the asset to the external service provider.</response>
    [HttpPost("{id:guid}/dispatch-to-external-service")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<DispatchToExternalServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DispatchToExternalServiceResponse>>DispatchToExternalService(
            [FromRoute] Guid id,
            [FromBody] DispatchToExternalServiceRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _dispatchToExternalServiceHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Hands over an asset directly to the assigned external service provider.
    /// </summary>
    /// <remarks>
    /// Registers the direct hand-over of the asset from headquarters to the
    /// external service provider assigned to the service case.
    ///
    /// The operation can be performed only if:
    /// - the service case exists,
    /// - the related asset exists,
    /// - the service case is awaiting external service dispatch,
    /// - a valid vendor is assigned to the service case,
    /// - the related asset is located at headquarters.
    ///
    /// Unlike dispatching the asset to an external service provider, this
    /// operation represents a direct hand-over without an intermediate
    /// transport state.
    ///
    /// The operation updates both the service case and the related asset.
    ///
    /// A HandedOverToExternalService event is recorded for the service case
    /// with the optional comment provided in the request. The asset status
    /// change is also recorded in the asset history.
    ///
    /// The authenticated user must have permission to manage all service cases.
    /// </remarks>
    /// <param name="id">The unique identifier of the service case.</param>
    /// <param name="request">
    /// The hand-over data, including an optional comment.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The updated service case and asset status information, including the
    /// assigned vendor and hand-over details.
    /// </returns>
    /// <response code="200">The asset was successfully handed over to the external service provider.</response>
    /// <response code="400">The service case identifier or request data is invalid.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to manage all service cases.</response>
    /// <response code="404">The service case or its related asset was not found.</response>
    /// <response code="409">The service case or asset is not in a valid state for direct hand-over to the external service provider, or no valid vendor is assigned.</response>
    /// <response code="500">An unexpected error occurred while handing over the asset to the external service provider.</response>
    [HttpPost("{id:guid}/hand-over-to-external-service")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<HandOverToExternalServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HandOverToExternalServiceResponse>>HandOverToExternalService(
            [FromRoute] Guid id,
            [FromBody]
            HandOverToExternalServiceRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _handOverToExternalServiceHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Confirms that an asset has been received by the external service provider.
    /// </summary>
    /// <remarks>
    /// Registers the receipt of an asset by the external service provider.
    ///
    /// The operation can be performed only if:
    /// - the service case exists,
    /// - the related asset exists,
    /// - the service case has been sent to the external service provider,
    /// - a valid vendor is assigned to the service case,
    /// - the related asset is currently in transit to the external service provider.
    ///
    /// The operation updates both the service case and the related asset to
    /// reflect that the asset has been received by the external service provider.
    ///
    /// A ReceivedByExternalService event is recorded for the service case
    /// with the optional comment provided in the request. The asset status
    /// change is also recorded in the asset history.
    ///
    /// The authenticated user must have permission to manage all service cases.
    /// </remarks>
    /// <param name="id">The unique identifier of the service case.</param>
    /// <param name="request">
    /// The external service receipt confirmation data, including an optional comment.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The updated service case and asset status information, including the
    /// assigned vendor and receipt confirmation details.
    /// </returns>
    /// <response code="200">The receipt of the asset by the external service provider was confirmed successfully.</response>
    /// <response code="400">The service case identifier or request data is invalid.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to manage all service cases.</response>
    /// <response code="404">The service case or its related asset was not found.</response>
    /// <response code="409">The service case or asset is not in a valid state for confirming receipt by the external service provider, or no valid vendor is assigned.</response>
    /// <response code="500">An unexpected error occurred while confirming receipt by the external service provider.</response>
        [HttpPost(
    "{id:guid}/confirm-received-by-external-service")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<ConfirmReceivedByExternalServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConfirmReceivedByExternalServiceResponse>>ConfirmReceivedByExternalService(
            [FromRoute] Guid id,
            [FromBody]
            ConfirmReceivedByExternalServiceRequest request,
            CancellationToken cancellationToken)
    {
        var response =  await _confirmReceivedByExternalServiceHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Records the return of an asset from the external service provider.
    /// </summary>
    /// <remarks>
    /// Registers that the asset has returned from the external service provider.
    ///
    /// The operation can be performed only if:
    /// - the service case exists,
    /// - the related asset exists,
    /// - the service case is currently in external service,
    /// - a valid vendor is assigned to the service case,
    /// - the related asset is currently in service.
    ///
    /// The operation updates both the service case and the related asset to
    /// reflect the return from the external service provider.
    ///
    /// A ReturnedFromExternalService event is recorded for the service case
    /// with the optional comment provided in the request. The asset status
    /// change is also recorded in the asset history.
    ///
    /// The authenticated user must have permission to manage all service cases.
    /// </remarks>
    /// <param name="id">The unique identifier of the service case.</param>
    /// <param name="request">
    /// The external service return data, including an optional comment.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The updated service case and asset status information, including the
    /// assigned vendor and return details.
    /// </returns>
    /// <response code="200">The return from the external service provider was recorded successfully.</response>
    /// <response code="400">The service case identifier or request data is invalid.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to manage all service cases.</response>
    /// <response code="404">The service case or its related asset was not found.</response>
    /// <response code="409">The service case or asset is not in a valid state for returning from external service, the service case and asset relationship is invalid, or no valid vendor is assigned.</response>
    /// <response code="500">An unexpected error occurred while recording the return from the external service provider.</response>
    [HttpPost("{id:guid}/return-from-external-service")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<ReturnFromExternalServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReturnFromExternalServiceResponse>>ReturnFromExternalService(
            [FromRoute] Guid id,
            [FromBody] ReturnFromExternalServiceRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _returnFromExternalServiceHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Completes the repair of an asset at headquarters.
    /// </summary>
    /// <remarks>
    /// Records that the repair of the asset has been completed at headquarters.
    ///
    /// The operation can be performed only if:
    /// - the service case exists,
    /// - the related asset exists,
    /// - the service case is under central inspection,
    /// - the related asset is currently located at headquarters,
    /// - the service case and asset relationship is valid.
    ///
    /// If the service case has an origin store, the origin store identifier
    /// must be valid and the asset must be assigned to that store.
    ///
    /// The operation updates the service case to reflect that the repair at
    /// headquarters has been completed.
    ///
    /// A RepairCompletedInHeadquarters event is recorded for the service case
    /// with the optional comment provided in the request.
    ///
    /// The authenticated user must have permission to manage all service cases.
    /// </remarks>
    /// <param name="id">The unique identifier of the service case.</param>
    /// <param name="request">
    /// The repair completion data, including an optional comment.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The updated service case information, including the related asset status
    /// and repair completion details.
    /// </returns>
    /// <response code="200">The repair at headquarters was completed successfully.</response>
    /// <response code="400">The service case identifier or request data is invalid.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to manage all service cases.</response>
    /// <response code="404">The service case or its related asset was not found.</response>
    /// <response code="409">The service case or asset is not in a valid state for completing the repair at headquarters, the service case and asset relationship is invalid, or the origin store information is inconsistent.</response>
    /// <response code="500">An unexpected error occurred while completing the repair at headquarters.</response>
    [HttpPost("{id:guid}/complete-repair-at-headquarters")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<CompleteRepairAtHeadquartersResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompleteRepairAtHeadquartersResponse>>CompleteRepairAtHeadquarters(
            [FromRoute] Guid id,
            [FromBody] CompleteRepairAtHeadquartersRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _completeRepairAtHeadquartersHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Resolves a service case after the asset has returned from external service.
    /// </summary>
    /// <remarks>
    /// Resolves a service case after the related asset has returned from the
    /// external service provider to headquarters.
    ///
    /// The operation can be performed only if:
    /// - the service case exists,
    /// - the related asset exists,
    /// - the service case is awaiting return,
    /// - the service case was not opened from a store and therefore has no origin store,
    /// - a valid vendor is assigned to the service case,
    /// - the related asset is currently located at headquarters.
    ///
    /// Service cases opened from a store cannot be resolved using this operation.
    /// They must first be returned to their origin store.
    ///
    /// The operation updates the service case to its resolved state.
    ///
    /// A Resolved event is recorded for the service case with the optional
    /// comment provided in the request.
    ///
    /// If provided, the comment must not be empty or whitespace and must not
    /// exceed 2000 characters.
    ///
    /// The authenticated user must have permission to manage all service cases.
    /// </remarks>
    /// <param name="id">The unique identifier of the service case.</param>
    /// <param name="request">
    /// The resolution data, including an optional comment.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The resolved service case information, including the related asset,
    /// assigned vendor, and resolution details.
    /// </returns>
    /// <response code="200">The service case was resolved successfully.</response>
    /// <response code="400">The service case identifier is invalid, or the comment is empty, whitespace, or exceeds 2000 characters.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to manage all service cases.</response>
    /// <response code="404">The service case or its related asset was not found.</response>
    /// <response code="409">The service case or asset is not in a valid state for resolution, the service case has an origin store, the service case and asset relationship is invalid, or no valid vendor is assigned.</response>
    /// <response code="500">An unexpected error occurred while resolving the service case.</response>
    [HttpPost("{id:guid}/resolve-after-external-service-return")]
    [Authorize(Policy = AuthorizationPolicies.ManageAllServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<ResolveAfterExternalServiceReturnResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResolveAfterExternalServiceReturnResponse>>ResolveAfterExternalServiceReturn(
            [FromRoute] Guid id,
            [FromBody] ResolveAfterExternalServiceReturnRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _resolveAfterExternalServiceReturnHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Closes a resolved service case.
    /// </summary>
    /// <remarks>
    /// Closes the specified service case.
    ///
    /// The operation can be performed only if:
    /// - the service case exists,
    /// - the service case is in the Resolved status.
    ///
    /// Closing the service case records the user who performed the operation
    /// and the time when the service case was closed.
    ///
    /// A Closed event is recorded for the service case with the optional
    /// comment provided in the request.
    ///
    /// If provided, the comment must not be empty or whitespace and must not
    /// exceed 2000 characters.
    ///
    /// The authenticated user must have permission to close service cases.
    /// </remarks>
    /// <param name="id">The unique identifier of the service case.</param>
    /// <param name="request">
    /// The service case closure data, including an optional comment.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The closed service case information, including the user who closed
    /// the case and the time of closure.
    /// </returns>
    /// <response code="200">The service case was closed successfully.</response>
    /// <response code="400">The service case identifier is invalid, or the comment is empty, whitespace, or exceeds 2000 characters.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to close service cases.</response>
    /// <response code="404">The service case was not found.</response>
    /// <response code="409">The service case is not in the Resolved status and therefore cannot be closed.</response>
    /// <response code="500">An unexpected error occurred while closing the service case.</response>
    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = AuthorizationPolicies.CloseServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<CloseServiceCaseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CloseServiceCaseResponse>>CloseServiceCase(
            [FromRoute] Guid id,
            [FromBody] CloseServiceCaseRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _closeServiceCaseHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves the service cases accessible to the current user.
    /// </summary>
    /// <remarks>
    /// Returns service cases according to the current user's store access scope.
    ///
    /// Users with access to all stores receive service cases without an
    /// origin store restriction. Store-scoped users receive only service
    /// cases associated with their assigned store as the origin store.
    ///
    /// Before retrieving the service cases, the operation verifies that the
    /// current user has permission to read service cases within the resolved
    /// store access scope.
    ///
    /// Each returned service case contains its case number, related asset,
    /// current status, issue type, origin store, assigned vendor, active state,
    /// creation information, and closure time when available.
    ///
    /// The authenticated user must have the ViewServiceCases permission.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The service cases accessible to the current user.</returns>
    /// <response code="200">The service cases were retrieved successfully.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to view service cases within the requested store scope.</response>
    /// <response code="500">An unexpected error occurred while retrieving the service cases.</response>
    [HttpGet]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(Policy = AuthorizationPolicies.ViewServiceCases)]
    [ProducesResponseType<GetServiceCasesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetServiceCasesResponse>>GetServiceCases(CancellationToken cancellationToken)
    {
        var response = await _getServiceCasesHandler.HandleAsync(cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves the detailed information of a service case.
    /// </summary>
    /// <remarks>
    /// Returns detailed information about the specified service case.
    ///
    /// Access to the service case is restricted according to the current user's
    /// permissions and store access scope.
    ///
    /// Users with access to all stores can retrieve any accessible service case.
    /// Store-scoped users can retrieve only service cases associated with their
    /// assigned store as the origin store.
    ///
    /// The response includes:
    /// - the service case identification and current status,
    /// - the related asset identifier,
    /// - issue information and description,
    /// - the origin store and assigned vendor,
    /// - creation and closure information,
    /// - the complete service case event history,
    /// - the actions currently available to the authenticated user.
    ///
    /// The available actions are determined based on the current user,
    /// permissions, and the current state of the service case.
    ///
    /// The authenticated user must have the ViewServiceCases permission.
    /// </remarks>
    /// <param name="id">The unique identifier of the service case.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detailed information of the requested service case.</returns>
    /// <response code="200">The service case details were retrieved successfully.</response>
    /// <response code="400">The service case identifier is invalid.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to view the service case or it is outside the user's store access scope.</response>
    /// <response code="404">The service case was not found.</response>
    /// <response code="500">An unexpected error occurred while retrieving the service case details.</response>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [Authorize(Policy = AuthorizationPolicies.ViewServiceCases)]
    [ProducesResponseType<GetServiceCaseDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetServiceCaseDetailsResponse>>GetServiceCaseDetails(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
    {
        var response = await _getServiceCaseDetailsHandler.HandleAsync(id, cancellationToken);

        return Ok(response);
    }
        /// <summary>
    /// Disposes of the asset associated with a service case.
    /// </summary>
    /// <remarks>
    /// Only active application administrators can perform this operation.
    /// The handler explicitly verifies the current application user's role.
    ///
    /// The following service case and asset status combinations are supported:
    /// - UnderCentralInspection with In_HQ,
    /// - AwaitingExternalServiceDispatch with In_HQ,
    /// - InExternalService with In_Service,
    /// - AwaitingReturn with In_HQ.
    ///
    /// The asset must not be assigned to an employee or an application user.
    /// The asset and service case must have a consistent relationship
    /// and origin store assignment.
    ///
    /// An asset located at an external service provider may remain there
    /// for donor parts without being returned to headquarters.
    ///
    /// The disposal reason is mandatory and cannot exceed 2000 characters
    /// after trimming.
    ///
    /// Successful execution:
    /// - changes the asset status to Disposed,
    /// - changes the service case status to Resolved,
    /// - records an AssetDisposed service case event,
    /// - records the asset status change and disposal reason in asset history.
    ///
    /// The event and asset history entries use the same UTC timestamp
    /// and performing administrator.
    ///
    /// Existing store and vendor assignments are preserved.
    /// Closing the service case remains a separate operation.
    ///
    /// Example request:
    ///
    ///     POST /api/service-cases/{id}/dispose-asset
    ///     {
    ///         "reason": "Beyond economical repair. Retained by the external service provider for donor parts."
    ///     }
    ///
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the service case.
    /// The associated asset is determined from this service case.
    /// </param>
    /// <param name="request">
    /// The request containing the mandatory disposal reason.
    /// </param>
    /// <param name="handler">
    /// The disposal handler resolved from the request's service scope.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The updated service case and asset statuses, recorded disposal reason,
    /// disposal timestamp, and performing administrator.
    /// </returns>
    /// <response code="200">
    /// The asset was disposed of and the service case was resolved successfully.
    /// </response>
    /// <response code="400">
    /// The service case identifier or disposal reason is invalid.
    /// </response>
    /// <response code="401">
    /// Authentication is required, or required identity information is missing.
    /// </response>
    /// <response code="403">
    /// The user lacks the required permission or scope, is inactive,
    /// or is not an application administrator.
    /// </response>
    /// <response code="404">
    /// The service case or its associated asset was not found.
    /// </response>
    /// <response code="409">
    /// The service case or asset state does not allow disposal,
    /// their relationship or origin store assignment is inconsistent,
    /// or the asset is still assigned to a user.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred while disposing of the asset.
    /// </response>
    [HttpPost("{id:guid}/dispose-asset")]
    [Authorize(Policy = AuthorizationPolicies.ManageServiceCases)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<DisposeServiceCaseAssetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DisposeServiceCaseAssetResponse>>DisposeServiceCaseAsset(
            [FromRoute] Guid id,
            [FromBody] DisposeServiceCaseAssetRequest request,
            [FromServices] DisposeServiceCaseAssetHandler handler,
            CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }
}