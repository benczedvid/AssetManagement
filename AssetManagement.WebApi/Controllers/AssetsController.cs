using AssetManagement.Application.Assets.CreateAsset;
using AssetManagement.Application.Assets.GetAsset;
using AssetManagement.Application.Assets.GetAssetDetails;
using AssetManagement.Application.Assets.GetAssets;
using AssetManagement.Application.Assets.UpdateAsset;
using AssetManagement.Application.Common.Authorization;
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

    public AssetsController(
        CreateAssetHandler createAssetHandler,
        GetAssetsHandler getAssetsHandler,
        GetAssetByIdHandler getAssetByIdHandler,
        UpdateAssetHandler updateAssetHandler,
        GetAssetDetailsHandler getAssetDetailsHandler)
    {
        ArgumentNullException.ThrowIfNull(createAssetHandler);
        ArgumentNullException.ThrowIfNull(getAssetsHandler);
        ArgumentNullException.ThrowIfNull(getAssetByIdHandler);
        ArgumentNullException.ThrowIfNull(updateAssetHandler);
        ArgumentNullException.ThrowIfNull(getAssetDetailsHandler);

        _createAssetHandler = createAssetHandler;
        _getAssetsHandler = getAssetsHandler;
        _getAssetByIdHandler = getAssetByIdHandler;
        _updateAssetHandler = updateAssetHandler;
        _getAssetDetailsHandler = getAssetDetailsHandler;
    }

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
}