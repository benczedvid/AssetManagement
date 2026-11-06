using AssetManagement.Application.AssetHistories;
using AssetManagement.Application.AssetHistories.GetAssetHistory;
using AssetManagement.Application.Assets.CreateAsset;
using AssetManagement.Application.Assets.DisposeAsset;
using AssetManagement.Application.Assets.GetAsset;
using AssetManagement.Application.Assets.GetAssetDetails;
using AssetManagement.Application.Assets.GetAssets;
using AssetManagement.Application.Assets.UpdateAsset;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.ServiceCases.DisposeServiceCaseAsset;
using AssetManagement.Application.Stores.GetStoreById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace AssetManagement.WebApi.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public sealed class AssetsController : ControllerBase
{
    private readonly CreateAssetHandler _createAssetHandler;
    private readonly GetAssetsHandler _getAssetsHandler;
    private readonly GetAssetByIdHandler _getAssetByIdHandler;
    private readonly UpdateAssetHandler _updateAssetHandler;
    private readonly GetAssetDetailsHandler _getAssetDetailsHandler;
    private readonly GetAssetHistoryHandler _getAssetHistoryHandler;

    public AssetsController(
        CreateAssetHandler createAssetHandler,
        GetAssetsHandler getAssetsHandler,
        GetAssetByIdHandler getAssetByIdHandler,
        UpdateAssetHandler updateAssetHandler,
        GetAssetDetailsHandler getAssetDetailsHandler,
        GetAssetHistoryHandler getAssetHistoryHandler)
    {
        ArgumentNullException.ThrowIfNull(createAssetHandler);
        ArgumentNullException.ThrowIfNull(getAssetsHandler);
        ArgumentNullException.ThrowIfNull(getAssetByIdHandler);
        ArgumentNullException.ThrowIfNull(updateAssetHandler);
        ArgumentNullException.ThrowIfNull(getAssetDetailsHandler);
        ArgumentNullException.ThrowIfNull(getAssetHistoryHandler);

        _createAssetHandler = createAssetHandler;
        _getAssetsHandler = getAssetsHandler;
        _getAssetByIdHandler = getAssetByIdHandler;
        _updateAssetHandler = updateAssetHandler;
        _getAssetDetailsHandler = getAssetDetailsHandler;
        _getAssetHistoryHandler = getAssetHistoryHandler;
    }

    /// <summary>
    /// Creates a new asset.
    /// </summary>
    /// <remarks>
    /// Creates a new asset using the provided asset data.
    ///
    /// Before creating the asset, the operation verifies that:
    /// - the assigned store exists,
    /// - the assigned user exists, if an AssignedUserId is provided,
    /// - the serial number is not already assigned to another asset.
    ///
    /// The authenticated user must have the ManageAssets permission.
    /// </remarks>
    /// <param name="request">The data required to create the asset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created asset.</returns>
    /// <response code="201">The asset was created successfully.</response>
    /// <response code="400">The request contains invalid asset data.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to manage assets.</response>
    /// <response code="404">The assigned store or assigned user was not found.</response>
    /// <response code="409">An asset with the specified serial number already exists.</response>
    /// <response code="500">An unexpected error occurred while creating the asset.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageAssets)]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<CreateAssetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateAssetResponse>> CreateAsset([FromBody] CreateAssetRequest request, CancellationToken cancellationToken)
    {
        var response = await _createAssetHandler.HandleAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetAssetById),
            new { id = response.AssetId },
            response);
    }

    /// <summary>
    /// Retrieves the list of assets accessible to the current user.
    /// </summary>
    /// <remarks>
    /// Returns assets according to the current user's access scope.
    ///
    /// Users with access to all stores receive all assets.
    /// Store-scoped users receive only assets assigned to their store.
    ///
    /// The returned assets are ordered by asset name and include additional
    /// information such as the assigned store number, employee name,
    /// and vendor name when available.
    ///
    /// The authenticated user must have the ViewAssets permission.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of assets accessible to the current user.</returns>
    /// <response code="200">The assets were retrieved successfully.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to view assets.</response>
    /// <response code="500">An unexpected error occurred while retrieving the assets.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ViewAssets)]
    [Produces("application/json")]
    [ProducesResponseType<IReadOnlyList<GetAssetsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<GetAssetsResponse>>> GetAssets(CancellationToken cancellationToken)
    {
        var assets = await _getAssetsHandler.HandleAsync(cancellationToken);

        return Ok(assets);
    }

    /// <summary>
    /// Retrieves an asset by its unique identifier.
    /// </summary>
    /// <remarks>
    /// Returns the asset identified by the specified ID.
    ///
    /// Access to the asset is restricted according to the current user's
    /// store access scope.
    ///
    /// Users with access to all stores can retrieve any asset.
    /// Store-scoped users can retrieve only assets assigned to their store.
    ///
    /// The authenticated user must have the ViewAssets permission.
    /// </remarks>
    /// <param name="id">The unique identifier of the asset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested asset.</returns>
    /// <response code="200">The asset was retrieved successfully.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to view assets.</response>
    /// <response code="404">The asset was not found or is not accessible within the user's store scope.</response>
    /// <response code="500">An unexpected error occurred while retrieving the asset.</response>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ViewAssets)]
    [Produces("application/json")]
    [ProducesResponseType<GetAssetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>( StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetAssetResponse>> GetAssetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var asset = await _getAssetByIdHandler.HandleAsync(id, cancellationToken); 

        return Ok(asset);
    }

    /// <summary>
    /// Updates an existing asset.
    /// </summary>
    /// <remarks>
    /// Updates the specified asset using the provided data.
    ///
    /// Access to the asset is restricted according to the current user's
    /// store access scope. Users with access to all stores can update any
    /// accessible asset, while store-scoped users can update only assets
    /// assigned to their own store.
    ///
    /// Store-scoped users cannot move an asset to another store.
    ///
    /// Before updating the asset, the operation verifies that:
    /// - the asset exists and is accessible to the current user,
    /// - the target store exists,
    /// - the assigned user exists, if an AssignedUserId is provided.
    ///
    /// Changes to the asset are recorded in the asset history, including
    /// the previous and new values, the user who performed the change,
    /// and the time of the change.
    ///
    /// The authenticated user must have the ManageAssets permission.
    /// </remarks>
    /// <param name="id">The unique identifier of the asset to update.</param>
    /// <param name="request">The asset data to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated asset.</returns>
    /// <response code="200">The asset was updated successfully.</response>
    /// <response code="400">The asset identifier or request data is invalid.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to update the asset or move it to another store.</response>
    /// <response code="404">The asset, target store, or assigned user was not found.</response>
    /// <response code="409">The requested update conflicts with the current state of the asset.</response>
    /// <response code="500">An unexpected error occurred while updating the asset.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageAssets)]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<UpdateAssetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UpdateAssetResponse>> UpdateAsset([FromRoute] Guid id, [FromBody] UpdateAssetRequest request, CancellationToken cancellationToken)
    {
        var asset = await _updateAssetHandler.HandleAsync(id, request, cancellationToken);

        return Ok(asset);
    }

    /// <summary>
    /// Retrieves detailed information about an asset.
    /// </summary>
    /// <remarks>
    /// Returns detailed information about the specified asset.
    ///
    /// Access to the asset is restricted according to the current user's
    /// store access scope. Users with access to all stores can retrieve
    /// details of any asset, while store-scoped users can retrieve
    /// details only for assets assigned to their own store.
    ///
    /// In addition to the asset data, the response includes:
    /// - the assigned store name,
    /// - the assigned employee number and name, when available,
    /// - the assigned vendor information, when available,
    /// - asset movements from the last 7 days.
    ///
    /// The movement history is ordered from newest to oldest and includes
    /// the related employee and store information when available.
    ///
    /// The authenticated user must have the ViewAssets permission.
    /// </remarks>
    /// <param name="id">The unique identifier of the asset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detailed information of the requested asset.</returns>
    /// <response code="200">The asset details were retrieved successfully.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to view assets.</response>
    /// <response code="404">The asset, assigned store, or assigned employee was not found.</response>
    /// <response code="500">An unexpected error occurred while retrieving the asset details.</response>
    [HttpGet("{id:guid}/details")]
    [Authorize(Policy = AuthorizationPolicies.ViewAssets)]
    [Produces("application/json")]
    [ProducesResponseType<GetAssetDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetAssetDetailsResponse>> GetAssetDetails([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _getAssetDetailsHandler.HandleAsync(id, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves the change history of an asset.
    /// </summary>
    /// <remarks>
    /// Returns the recorded change history of the specified asset.
    ///
    /// Access to the asset is restricted according to the current user's
    /// store access scope. Users with access to all stores can retrieve
    /// the history of any asset, while store-scoped users can retrieve
    /// the history only for assets assigned to their own store.
    ///
    /// Each history entry contains the performed action, the changed property,
    /// its previous and new values, the user who performed the change,
    /// and the time when the change occurred.
    ///
    /// The authenticated user must have the ViewAssets permission.
    /// </remarks>
    /// <param name="id">The unique identifier of the asset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recorded change history of the asset.</returns>
    /// <response code="200">The asset history was retrieved successfully.</response>
    /// <response code="400">The asset identifier is invalid.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to view assets.</response>
    /// <response code="404">The asset was not found or is not accessible within the user's store scope.</response>
    /// <response code="500">An unexpected error occurred while retrieving the asset history.</response>
    [HttpGet("{id:guid}/history")]
    [Authorize(Policy = AuthorizationPolicies.ViewAssets)]
    [Produces("application/json")]
    [ProducesResponseType<IReadOnlyList<AssetHistoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<AssetHistoryResponse>>>GetAssetHistory([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _getAssetHistoryHandler.HandleAsync(id, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Disposes of an existing asset.
    /// </summary>
    /// <remarks>
    /// Marks the asset as disposed without deleting its record.
    ///
    /// Only active application administrators can perform this operation.
    /// The handler explicitly verifies the current application user's role.
    ///
    /// The asset must meet all of the following conditions:
    /// - its status is In_Store, In_HQ, or In_Service,
    /// - it is not assigned to an employee or an application user,
    /// - it has not already been disposed of,
    /// - it does not have an active service case.
    ///
    /// An asset located at an external service provider may be disposed of
    /// without being returned to headquarters, including when it remains
    /// at the service provider for donor parts.
    ///
    /// Assets with an active service case must be disposed of through
    /// the service case workflow instead of this standalone endpoint.
    ///
    /// The disposal reason is mandatory and cannot exceed 2000 characters
    /// after trimming.
    ///
    /// The status change and disposal reason are recorded in the asset
    /// history with the performing administrator and a shared UTC timestamp.
    /// Existing store and vendor assignments are preserved.
    ///
    /// Example request:
    ///
    ///     POST /api/assets/{id}/dispose
    ///     {
    ///         "reason": "Beyond economical repair. Retained by the external service provider for donor parts."
    ///     }
    ///
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the asset to dispose of.
    /// </param>
    /// <param name="request">
    /// The request containing the mandatory disposal reason.
    /// </param>
    /// <param name="handler">
    /// The handler resolved from the request's service scope.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The disposed asset status, recorded reason, disposal timestamp,
    /// and performing administrator.
    /// </returns>
    /// <response code="200">
    /// The asset was disposed of successfully.
    /// </response>
    /// <response code="400">
    /// The asset identifier or disposal reason is invalid.
    /// </response>
    /// <response code="401">
    /// Authentication is required to perform this operation.
    /// </response>
    /// <response code="403">
    /// The user lacks the required permission, is inactive,
    /// or is not an application administrator.
    /// </response>
    /// <response code="404">
    /// The asset was not found.
    /// </response>
    /// <response code="409">
    /// The asset has an active service case, has already been disposed of,
    /// is still assigned to a user, or its current status does not allow disposal.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred while disposing of the asset.
    /// </response>
    [HttpPost("{id:guid}/dispose")]
    [Authorize(Policy = AuthorizationPolicies.ManageAssets)]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<DisposeAssetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DisposeAssetResponse>> DisposeAsset(
        [FromRoute] Guid id,
        [FromBody] DisposeAssetRequest request,
        [FromServices] DisposeAssetHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }


}