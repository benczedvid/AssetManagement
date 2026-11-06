using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Employees;
using AssetManagement.Domain.Entities.Employees;

namespace AssetManagement.Application.Employees.GetEmployees;

public sealed class GetEmployeesHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserAccessScopeResolver _userAccessScopeResolver;

    public GetEmployeesHandler(IEmployeeRepository employeeRepository,
        IUserAccessScopeResolver userAccessScopeResolver)
    {
        ArgumentNullException.ThrowIfNull(employeeRepository);
        ArgumentNullException.ThrowIfNull(userAccessScopeResolver);

        _employeeRepository = employeeRepository;
        _userAccessScopeResolver = userAccessScopeResolver;
    }

    public async Task<IReadOnlyList<GetEmployeesResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var accessScope = await _userAccessScopeResolver.ResolveAsync( cancellationToken);

        IReadOnlyList<Employee> employees;

        if (accessScope.CanAccessAllStores)
        {
            employees = await _employeeRepository.GetAllAsync(cancellationToken);
        }
        else
        {
            if (!accessScope.StoreId.HasValue)
            {
                throw new InvalidOperationException("The current user does not have a valid store assignment.");
            }

            employees = await _employeeRepository.GetAllByStoreIdAsync(accessScope.StoreId.Value,cancellationToken);
        }

        return employees
            .Select(employee => new GetEmployeesResponse(
                EmployeeNumber: employee.EmployeeNumber,
                FullName: employee.FullName,
                EmploymentStartDate: employee.EmploymentStartDate,
                EmploymentEndDate: employee.EmploymentEndDate,
                StoreNumber: employee.StoreNumber,
                OrganizationType: employee.OrganizationType,
                PositionCode: employee.PositionCode,
                Position: employee.Position,
                PhoneNumber: employee.PhoneNumber,
                Email: employee.Email))
            .ToArray();
    }
}