using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Employees;
using AssetManagement.Domain.Entities.Employees;

namespace AssetManagement.Application.Employees.GetEmployeeByEmployeeNumber
{
    public sealed class GetEmployeeByEmployeeNumberHandler
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IUserAccessScopeResolver _userAccessScopeResolver;

        public GetEmployeeByEmployeeNumberHandler(IEmployeeRepository employeeRepository, IUserAccessScopeResolver userAccessScopeResolver)
        {
            ArgumentNullException.ThrowIfNull(employeeRepository);
            ArgumentNullException.ThrowIfNull(userAccessScopeResolver);

            _employeeRepository = employeeRepository;
            _userAccessScopeResolver = userAccessScopeResolver;
        }

        public async Task<GetEmployeeByEmployeeNumberResponse?> HandleAsync(string employeeNumber, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);

            var normalizedEmployeeNumber = employeeNumber.Trim();

            var accessScope = await _userAccessScopeResolver.ResolveAsync(cancellationToken);

            Employee? employee;

            if (accessScope.CanAccessAllStores)
            {
                employee = await _employeeRepository.GetByEmployeeNumberAsync(normalizedEmployeeNumber, cancellationToken);
            }
            else
            {
                if (!accessScope.StoreId.HasValue)
                {
                    throw new InvalidOperationException("The current user does not have a valid store assignment.");
                }

                employee = await _employeeRepository.GetByEmployeeNumberAndStoreIdAsync(normalizedEmployeeNumber, accessScope.StoreId.Value, cancellationToken);
            }

            if (employee is null)
            {
                return null;
            }

            return new GetEmployeeByEmployeeNumberResponse(
                EmployeeNumber: employee.EmployeeNumber,
                FullName: employee.FullName,
                EmploymentStartDate: employee.EmploymentStartDate,
                EmploymentEndDate: employee.EmploymentEndDate,
                StoreNumber: employee.StoreNumber,
                OrganizationType: employee.OrganizationType,
                PositionCode: employee.PositionCode,
                Position: employee.Position,
                PhoneNumber: employee.PhoneNumber,
                Email: employee.Email);
        }
    }
}