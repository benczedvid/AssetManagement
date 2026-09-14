using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Dashboard.GetStoreDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.WebApi.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly GetStoreDashboardHandler _handler;

    public DashboardController(GetStoreDashboardHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpGet("stores")]
    [Authorize(Policy = AuthorizationPolicies.ViewDashboard)]
    [ProducesResponseType(typeof(IReadOnlyList<StoreDashboardResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StoreDashboardResponse>>>GetStoreDashboardAsync(CancellationToken cancellationToken)
    {
        var response = await _handler.HandleAsync(cancellationToken);

        return Ok(response);
    }
}