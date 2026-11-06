using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Packages.AddPackageAsset;
using AssetManagement.Application.Packages.AssignCourier;
using AssetManagement.Application.Packages.CancelPackage;
using AssetManagement.Application.Packages.CreatePackage;
using AssetManagement.Application.Packages.DispatchPackage;
using AssetManagement.Application.Packages.GetPackageById;
using AssetManagement.Application.Packages.GetPackages;
using AssetManagement.Application.Packages.ReceivePackage;
using AssetManagement.Application.Packages.RemovePackageAsset;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace AssetManagement.WebApi.Controllers;

/// <summary>
/// Provides operations for creating, dispatching and receiving asset packages.
/// </summary>
[ApiController]
[Route("api/packages")]
[Authorize]
[RequiredScope(
    RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public sealed class PackagesController : ControllerBase
{
    private readonly CreatePackageHandler _createPackageHandler;
    private readonly GetPackageByIdHandler _getPackageByIdHandler;
    private readonly GetPackagesHandler _getPackagesHandler;
    private readonly AssignCourierHandler _assignCourierHandler;
    private readonly DispatchPackageHandler _dispatchPackageHandler;
    private readonly ReceivePackageHandler _receivePackageHandler;
    private readonly CancelPackageHandler _cancelPackageHandler;
    private readonly AddPackageAssetHandler _addPackageAssetHandler;
private readonly RemovePackageAssetHandler _removePackageAssetHandler;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PackagesController"/> class.
    /// </summary>
    /// <param name="createPackageHandler">
    /// The handler responsible for creating packages.
    /// </param>
    /// <param name="getPackagesHandler">
    /// The handler responsible for retrieving the package list.
    /// </param>
    /// <param name="getPackageByIdHandler">
    /// The handler responsible for retrieving package details.
    /// </param>
    /// <param name="assignCourierHandler">
    /// The handler responsible for assigning, changing or removing
    /// package couriers.
    /// </param>
    /// <param name="dispatchPackageHandler">
    /// The handler responsible for dispatching packages.
    /// </param>
    /// <param name="receivePackageHandler">
    /// The handler responsible for receiving packages.
    /// </param>
    /// <param name="cancelPackageHandler">
    /// The handler responsible for cancelling packages.
    /// </param>
    /// <param name="addPackageAssetHandler"></param>
    /// <param name="removePackageAssetHandler"></param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required handler is null.
    /// </exception>
    public PackagesController(
        CreatePackageHandler createPackageHandler,
        GetPackageByIdHandler getPackageByIdHandler,
        GetPackagesHandler getPackagesHandler,
        AssignCourierHandler assignCourierHandler,
        DispatchPackageHandler dispatchPackageHandler,
        ReceivePackageHandler receivePackageHandler,
        CancelPackageHandler cancelPackageHandler,
        AddPackageAssetHandler addPackageAssetHandler,
        RemovePackageAssetHandler removePackageAssetHandler)
    {
        ArgumentNullException.ThrowIfNull(createPackageHandler);
        ArgumentNullException.ThrowIfNull(getPackageByIdHandler);
        ArgumentNullException.ThrowIfNull(getPackagesHandler);
        ArgumentNullException.ThrowIfNull(assignCourierHandler);
        ArgumentNullException.ThrowIfNull(dispatchPackageHandler);
        ArgumentNullException.ThrowIfNull(receivePackageHandler);
        ArgumentNullException.ThrowIfNull(cancelPackageHandler);
        ArgumentNullException.ThrowIfNull(addPackageAssetHandler);
        ArgumentNullException.ThrowIfNull(removePackageAssetHandler);

        _createPackageHandler = createPackageHandler;
        _getPackageByIdHandler = getPackageByIdHandler;
        _getPackagesHandler = getPackagesHandler;
        _assignCourierHandler = assignCourierHandler;
        _dispatchPackageHandler = dispatchPackageHandler;
        _receivePackageHandler = receivePackageHandler;
        _cancelPackageHandler = cancelPackageHandler;
        _addPackageAssetHandler = addPackageAssetHandler;
        _removePackageAssetHandler = removePackageAssetHandler;
    }

    /// <summary>
    /// Creates a new package for transporting assets between a store and headquarters.
    /// </summary>
    /// <remarks>
    /// Creates a package containing one or more assets.
    ///
    /// Headquarters is represented by the store whose store number is 999.
    ///
    /// The following transport directions are supported:
    /// - from a regular store to headquarters,
    /// - from headquarters to a regular store.
    ///
    /// A package cannot be created:
    /// - between two regular stores,
    /// - from headquarters to headquarters,
    /// - with identical source and destination stores,
    /// - without at least one asset,
    /// - with duplicate asset identifiers,
    /// - with an asset assigned to another store,
    /// - with a disposed asset,
    /// - with an asset assigned to an employee,
    /// - with an asset already included in an active package.
    ///
    /// When the source store is headquarters, every asset must be in the In_HQ status.
    /// When the source is a regular store, every asset must be in the In_Store status.
    ///
    /// Creating the package does not change the status or assigned store of the included assets. Asset status changes occur only
    /// when the package is dispatched.
    ///
    /// A courier may optionally be selected from active application users. The package receives a generated business identifier
    /// using the following format:
    ///
    /// PKG-{YEAR}-{SEQUENCE}
    ///
    /// Example:
    ///
    /// PKG-2026-000001
    /// </remarks>
    /// <param name="request">
    /// The data required to create the package.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// The newly created package.
    /// </returns>
    /// <response code="201">
    /// The package was created successfully.
    /// </response>
    /// <response code="400">
    /// The request contains invalid identifiers, duplicate assets,
    /// no assets, an invalid comment or an unsupported route.
    /// </response>
    /// <response code="401">
    /// Authentication is required to perform this operation.
    /// </response>
    /// <response code="403">
    /// The current user does not have permission to create the package or cannot access the source store.
    /// </response>
    /// <response code="404">
    /// The source store, destination store, asset or selected courier was not found.
    /// </response>
    /// <response code="409">
    /// An asset is not in a valid state, is assigned to an employee, is assigned to another store or already belongs to an active package.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred while creating the package.
    /// </response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManagePackages)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<CreatePackageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreatePackageResponse>>CreatePackage([FromBody] CreatePackageRequest request, CancellationToken cancellationToken)
    {
        var response = await _createPackageHandler.HandleAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetPackageById), new
            {
                id = response.Id
            },
            response);
    }

        /// <summary>
    /// Gets packages visible to the current user.
    /// </summary>
    /// <remarks>
    /// Returns packages according to the current user's store access scope.
    ///
    /// Users with global store access receive all packages.
    ///
    /// Store-scoped users only receive packages where their assigned store is either:
    ///
    /// - the source store; or
    /// - the destination store.
    ///
    /// The returned items contain package summary information, including the package number, current status, route, optional
    /// courier, asset count and lifecycle timestamps.
    ///
    /// The complete event history is not included in the package list. Use GET /api/packages/{id} to retrieve the detailed
    /// package information.
    /// </remarks>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The packages visible to the current user.
    /// </returns>
    /// <response code="200">
    /// The package list was retrieved successfully.
    /// </response>
    /// <response code="401">
    /// Authentication is required to perform this operation.
    /// </response>
    /// <response code="403">
    /// The current user does not have a valid application role or store access scope.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred while retrieving the package list.
    /// </response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ViewPackages)]
    [Produces("application/json")]
    [ProducesResponseType<GetPackagesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetPackagesResponse>>GetPackages(CancellationToken cancellationToken)
    {
        var response = await _getPackagesHandler.HandleAsync(cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Gets the detailed information of a package.
    /// </summary>
    /// <remarks>
    /// Returns a package together with its included assets and complete lifecycle event history.
    ///
    /// Users with global store access can retrieve any package.
    ///
    /// Store-scoped users can only retrieve packages where their assigned store is either the source store or the destination store.
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// The detailed package information.
    /// </returns>
    /// <response code="200">
    /// The package was retrieved successfully.
    /// </response>
    /// <response code="400">
    /// The package identifier is empty or otherwise invalid.
    /// </response>
    /// <response code="401">
    /// Authentication is required to perform this operation.
    /// </response>
    /// <response code="403">
    /// The current user is not allowed to access the package.
    /// </response>
    /// <response code="404">
    /// The requested package was not found.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred while retrieving the package.
    /// </response>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ViewPackages)]
    [Produces("application/json")]
    [ProducesResponseType<GetPackageByIdResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetPackageByIdResponse>>GetPackageById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _getPackageByIdHandler.HandleAsync(id, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Assigns, changes or removes the courier of a package.
    /// </summary>
    /// <remarks>
    /// The courier assignment can only be changed while the package is in the Created status.
    ///
    /// To assign a courier, provide the identifier of an active application user.
    ///
    /// To change the courier, provide the identifier of a different active application user.
    ///
    /// To remove the currently assigned courier, send null as the courierUserId value.
    ///
    /// Store-scoped users can only change the courier of a package whose source store is their assigned store.
    ///
    /// Users with global store access can change the courier of any package that is still in the Created status.
    ///
    /// Assigning, changing or removing a courier does not change the package status and does not change the status of any included
    /// asset.
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="request">
    /// The courier assignment request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The updated courier assignment.
    /// </returns>
    /// <response code="200">
    /// The courier assignment was updated successfully.
    /// </response>
    /// <response code="400">
    /// The package identifier, courier identifier or comment is invalid.
    /// </response>
    /// <response code="401">
    /// Authentication is required to perform this operation.
    /// </response>
    /// <response code="403">
    /// The current user is not allowed to change the courier of the
    /// package.
    /// </response>
    /// <response code="404">
    /// The package or selected courier could not be found.
    /// </response>
    /// <response code="409">
    /// The package is not in the Created status, the selected courier is inactive, the same courier is already assigned or there is
    /// no courier to remove.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred while updating the courier.
    /// </response>
    [HttpPut("{id:guid}/courier")]
    [Authorize(Policy = AuthorizationPolicies.ManagePackages)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<AssignCourierResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssignCourierResponse>>AssignCourier(
            [FromRoute] Guid id,
            [FromBody] AssignCourierRequest request,
            CancellationToken cancellationToken)
    {
        var response =  await _assignCourierHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Dispatches a package from its source store.
    /// </summary>
    /// <remarks>
    /// Dispatches a package that is currently in the Created status.
    ///
    /// Headquarters is represented by the store whose store number is 999.
    ///
    /// For packages sent from a regular store to headquarters, every included asset must:
    ///
    /// - be assigned to the source store;
    /// - be in the In_Store status;
    /// - not be assigned to an employee;
    /// - not be assigned to an application user;
    /// - not be disposed.
    ///
    /// When such a package is dispatched, the included assets are changed from In_Store to Route_To_HQ.
    ///
    /// For packages sent from headquarters to a regular store, every included asset must:
    ///
    /// - be assigned to the source store;
    /// - be in the In_HQ status;
    /// - not be assigned to an employee;
    /// - not be assigned to an application user;
    /// - not be disposed.
    ///
    /// When such a package is dispatched, the included assets are changed from In_HQ to Route_To_Store.
    ///
    /// Dispatching the package:
    ///
    /// - changes the package status from Created to InTransit;
    /// - records the dispatch date and user;
    /// - updates the status of every included asset;
    /// - creates an asset history record for every asset status change;
    /// - records a Dispatched package event.
    ///
    /// Store-scoped users can only dispatch packages whose source store is their assigned store.
    ///
    /// Users with global store access can dispatch any valid package.
    ///
    /// The package must contain at least one asset before dispatch.
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="request">
    /// The package dispatch request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The dispatched package and the resulting asset statuses.
    /// </returns>
    /// <response code="200">
    /// The package was dispatched successfully.
    /// </response>
    /// <response code="400">
    /// The package identifier or dispatch comment is invalid.
    /// </response>
    /// <response code="401">
    /// Authentication is required to perform this operation.
    /// </response>
    /// <response code="403">
    /// The current user is not allowed to dispatch the package.
    /// </response>
    /// <response code="404">
    /// The package, source store, destination store or an included asset could not be found.
    /// </response>
    /// <response code="409">
    /// The package or an included asset is not in a valid state for dispatch.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred while dispatching the package.
    /// </response>
    [HttpPost("{id:guid}/dispatch")]
    [Authorize(Policy = AuthorizationPolicies.ManagePackages)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<DispatchPackageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DispatchPackageResponse>>DispatchPackage(
            [FromRoute] Guid id,
            [FromBody] DispatchPackageRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _dispatchPackageHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Receives a package at its destination store.
    /// </summary>
    /// <remarks>
    /// Only packages in the InTransit status can be received.
    ///
    /// The receiving store identifier must match the destination store configured on the package.
    ///
    /// Store-scoped users can only receive packages whose destination store is their assigned store.
    ///
    /// For packages received at headquarters, included assets change from Route_To_HQ to In_HQ.
    ///
    /// For packages received at a regular store, included assets change from Route_To_Store to In_Store.
    ///
    /// Receiving a package does not change the AssignedStoreId value of any included asset. AssignedStoreId identifies the store
    /// responsible for the asset, not its current physical location.
    ///
    /// When an included asset has an active service case:
    ///
    /// - receipt at headquarters changes a service case awaiting
    ///   central receipt to UnderCentralInspection;
    /// - receipt at the origin store applies the existing service case
    ///   return-to-store transition.
    ///
    /// The entire package is received in one operation. Partial package receipt is not supported.
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="request">
    /// The package receipt request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The received package information.
    /// </returns>
    /// <response code="200">
    /// The package was received successfully.
    /// </response>
    /// <response code="400">
    /// The package identifier, receiving store identifier or comment
    /// is invalid.
    /// </response>
    /// <response code="401">
    /// Authentication is required.
    ///     /// </response>
    /// <response code="403">
    /// The current user cannot receive the package.
    /// </response>
    /// <response code="404">
    /// The package, destination store, included asset or related entity was not found.
    /// </response>
    /// <response code="409">
    /// The package, an included asset or a related service case is not in a valid state for receipt.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred.
    /// </response>
    [HttpPost("{id:guid}/receive")]
    [Authorize(Policy = AuthorizationPolicies.ManagePackages)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<ReceivePackageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReceivePackageResponse>>ReceivePackage(
            [FromRoute] Guid id,
            [FromBody] ReceivePackageRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _receivePackageHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Cancels a package before dispatch.
    /// </summary>
    /// <remarks>
    /// Only packages in the Created status can be cancelled.
    ///
    /// The cancellation reason is required and cannot exceed 2000 characters.
    ///
    /// Cancelling a package:
    ///
    /// - changes the package status from Created to Cancelled;
    /// - records the cancellation date and user;
    /// - records a Cancelled package event;
    /// - releases the included assets for use in another package.
    ///
    /// Cancelling a package does not:
    ///
    /// - change the status of any included asset;
    /// - change the AssignedStoreId of any included asset;
    /// - delete package items or package events;
    /// - cancel or otherwise modify related service cases.
    ///
    /// Store-scoped users can only cancel packages whose source store is their assigned store.
    ///
    /// Users with global store access can cancel any package that is still in the Created status.
    ///
    /// Packages in the InTransit, Received or Cancelled status cannot be cancelled.
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="request">
    /// The package cancellation request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The cancelled package information.
    /// </returns>
    /// <response code="200">
    /// The package was cancelled successfully.
    /// </response>
    /// <response code="400">
    /// The package identifier or cancellation reason is invalid.
    /// </response>
    /// <response code="401">
    /// Authentication is required.
    /// </response>
    /// <response code="403">
    /// The current user cannot cancel the package.
    /// </response>
    /// <response code="404">
    /// The requested package was not found.
    /// </response>
    /// <response code="409">
    /// The package is not in the Created status.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred while cancelling the package.
    /// </response>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.ManagePackages)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<CancelPackageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CancelPackageResponse>>CancelPackage(
            [FromRoute] Guid id,
            [FromBody] CancelPackageRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _cancelPackageHandler.HandleAsync(id, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Adds an asset to an existing package.
    /// </summary>
    /// <remarks>
    /// Adds an asset to a package that has not yet been dispatched.
    ///
    /// The package must be in the Created status.
    ///
    /// The operation validates:
    /// - the current user's permission to manage the source store,
    /// - the asset's current location and status,
    /// - employee and application user assignments,
    /// - existing active package membership,
    /// - related active service case state and transport direction.
    ///
    /// Adding an asset does not change the asset status,
    /// assigned store or related service case status.
    ///
    /// The package number remains unchanged.
    ///
    /// An AssetAdded event is recorded in the package history.
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="request">
    /// The asset to add to the package.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// The updated package summary after adding the asset.
    /// </returns>
    /// <response code="200">
    /// The asset was added successfully.
    /// </response>
    /// <response code="400">
    /// The request contains an invalid package or asset identifier.
    /// </response>
    /// <response code="401">
    /// Authentication is required.
    /// </response>
    /// <response code="403">
    /// The current user is not authorized to modify the package.
    /// </response>
    /// <response code="404">
    /// The package, asset or required store was not found.
    /// </response>
    /// <response code="409">
    /// The package is not in Created status, the asset is already
    /// included in an active package, or the asset and service case
    /// states do not allow the operation.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred.
    /// </response>
    [HttpPost("{id:guid}/assets")]
    [Authorize(Policy = AuthorizationPolicies.ManagePackages)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<AddPackageAssetResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AddPackageAssetResponse>> AddPackageAsset(
        [FromRoute] Guid id,
        [FromBody] AddPackageAssetRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _addPackageAssetHandler.HandleAsync(
            id,
            request,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Removes an asset from an existing package.
    /// </summary>
    /// <remarks>
    /// Removes an asset from a package that has not yet been dispatched.
    ///
    /// The package must be in the Created status.
    ///
    /// The current user must have permission to manage the package
    /// at its source store.
    ///
    /// The specified asset must currently belong to the package.
    ///
    /// Removing an asset:
    /// - does not change the package number,
    /// - does not change the package status,
    /// - does not change the asset status,
    /// - does not change any related service case status.
    ///
    /// An AssetRemoved event is recorded in the package history.
    ///
    /// The last asset may also be removed, but an empty package
    /// cannot subsequently be dispatched.
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="assetId">
    /// The unique identifier of the asset to remove.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// The updated package summary after removing the asset.
    /// </returns>
    /// <response code="200">
    /// The asset was removed successfully.
    /// </response>
    /// <response code="400">
    /// An identifier is invalid.
    /// </response>
    /// <response code="401">
    /// Authentication is required.
    /// </response>
    /// <response code="403">
    /// The current user is not authorized to modify the package.
    /// </response>
    /// <response code="404">
    /// The package was not found.
    /// </response>
    /// <response code="409">
    /// The package is not in Created status or the asset is not
    /// included in the package.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred.
    /// </response>
    [HttpDelete("{id:guid}/assets/{assetId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManagePackages)]
    [Produces("application/json")]
    [ProducesResponseType<RemovePackageAssetResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RemovePackageAssetResponse>> RemovePackageAsset(
        [FromRoute] Guid id,
        [FromRoute] Guid assetId,
        CancellationToken cancellationToken)
    {
        var response = await _removePackageAssetHandler.HandleAsync(
            id,
            assetId,
            cancellationToken);

        return Ok(response);
    }
}