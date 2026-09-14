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