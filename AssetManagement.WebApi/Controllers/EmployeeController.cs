using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Employees.GetEmployeeByEmployeeNumber;
using AssetManagement.Application.Employees.GetEmployees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace AssetManagement.WebApi.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public sealed class EmployeesController : ControllerBase
{
    private readonly GetEmployeesHandler _getEmployeesHandler;
    private readonly GetEmployeeByEmployeeNumberHandler _getEmployeeByEmployeeNumberHandler;

    public EmployeesController(GetEmployeesHandler getEmployeesHandler, GetEmployeeByEmployeeNumberHandler getEmployeeByEmployeeNumberHandler)
    {
        ArgumentNullException.ThrowIfNull(getEmployeesHandler);
        ArgumentNullException.ThrowIfNull(getEmployeeByEmployeeNumberHandler);

        _getEmployeesHandler = getEmployeesHandler;
        _getEmployeeByEmployeeNumberHandler = getEmployeeByEmployeeNumberHandler;
    }

    /// <summary>
    /// Retrieves the employees accessible to the current user.
    /// </summary>
    /// <remarks>
    /// Returns employee data according to the current user's store access scope.
    ///
    /// Users with access to all stores receive all employees.
    /// Store-scoped users receive only employees assigned to their own store.
    ///
    /// The response includes employment, organization, position, store,
    /// phone number, and email information when available.
    ///
    /// The authenticated user must have the ViewEmployees permission.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of employees accessible to the current user.</returns>
    /// <response code="200">The employees were retrieved successfully.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to view employees.</response>
    /// <response code="500">An unexpected error occurred while retrieving the employees.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ViewEmployees)]
    [Produces("application/json")]
    [ProducesResponseType<IReadOnlyList<GetEmployeesResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<GetEmployeesResponse>>>
        GetAll(CancellationToken cancellationToken)
    {
        var employees = await _getEmployeesHandler.HandleAsync(cancellationToken);

        return Ok(employees);
    }

    /// <summary>
    /// Retrieves an employee by employee number.
    /// </summary>
    /// <remarks>
    /// Returns the employee identified by the specified employee number.
    ///
    /// The employee number is trimmed before the lookup is performed.
    ///
    /// Access to employee data is restricted according to the current user's
    /// store access scope. Users with access to all stores can retrieve any
    /// employee, while store-scoped users can retrieve only employees
    /// assigned to their own store.
    ///
    /// The authenticated user must have the ViewEmployees permission.
    /// </remarks>
    /// <param name="employeeNumber">The employee number used to identify the employee.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The employee matching the specified employee number.</returns>
    /// <response code="200">The employee was retrieved successfully.</response>
    /// <response code="400">The employee number is null, empty, or consists only of whitespace.</response>
    /// <response code="401">Authentication is required to perform this operation.</response>
    /// <response code="403">The user does not have permission to view employees.</response>
    /// <response code="404">The employee was not found or is not accessible within the user's store scope.</response>
    /// <response code="500">An unexpected error occurred while retrieving the employee.</response>
    [HttpGet("{employeeNumber}")]
    [Authorize(Policy = AuthorizationPolicies.ViewEmployees)]
    [Produces("application/json")]
    [ProducesResponseType<GetEmployeeByEmployeeNumberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetEmployeeByEmployeeNumberResponse>>GetByEmployeeNumber([FromRoute] string employeeNumber, CancellationToken cancellationToken)
    {
        var employee = await _getEmployeeByEmployeeNumberHandler.HandleAsync(employeeNumber, cancellationToken);

        if (employee is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Employee not found.",
                Detail =$"No employee was found with employee number '{employeeNumber}'."
            });
        }

        return Ok(employee);
    }
}