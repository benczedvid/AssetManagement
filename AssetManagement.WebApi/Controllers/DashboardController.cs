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

    /// <summary>
    /// Retrieves the store dashboard data.
    /// </summary>
    /// <remarks>
    /// Returns PDT asset statistics grouped by store.
    ///
    /// The returned data is restricted according to the current user's
    /// store access scope. Users with access to all stores receive dashboard
    /// data for all stores, while store-scoped users receive data only for
    /// their assigned store.
    ///
    /// Only PDT assets that are not disposed are included in the statistics.
    ///
    /// For each store, the response contains:
    /// - the total number of active PDTs,
    /// - the number of PDTs currently used in the store,
    /// - the number of PDTs currently in service,
    /// - the number of PDTs currently available in the store.
    ///
    /// The results are ordered by store number.
    ///
    /// The authenticated user must have the ViewDashboard permission.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dashboard statistics grouped by store.</returns>
    /// <response code="200">The store dashboard data was retrieved successfully.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to view the dashboard.</response>
    /// <response code="500">An unexpected error occurred while retrieving the dashboard data.</response>
    [HttpGet("stores")]
    [Authorize(Policy = AuthorizationPolicies.ViewDashboard)]
    [ProducesResponseType(typeof(IReadOnlyList<StoreDashboardResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StoreDashboardResponse>>>GetStoreDashboardAsync(CancellationToken cancellationToken)
    {
        var response = await _handler.HandleAsync(cancellationToken);

        return Ok(response);
    }
}