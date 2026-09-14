using AssetManagement.Application.Common.Interfaces.Employees;
using AssetManagement.Domain.Entities.Employees;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories
{
    public sealed class EmployeeRepository : IEmployeeRepository
    {
        private readonly AppDbContext _appDbContext;

        public EmployeeRepository(AppDbContext appDbContext)
        {
            ArgumentNullException.ThrowIfNull(appDbContext);

            _appDbContext = appDbContext;
        }

        public async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _appDbContext.Employees
                .AsNoTracking()
                .OrderBy(employee => employee.FullName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Employee>> GetAllByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("The store identifier cannot be empty.", nameof(storeId));
            }

            return await (
                from employee in _appDbContext.Employees.AsNoTracking()
                join store in _appDbContext.Stores.AsNoTracking()
                    on employee.StoreNumber equals store.StoreNumber
                where store.Id == storeId
                orderby employee.FullName
                select employee)
                .ToListAsync(cancellationToken);
        }

        public async Task<Employee?> GetByEmployeeNumberAndStoreIdAsync(string employeeNumber, Guid storeId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);

            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("The store identifier cannot be empty.", nameof(storeId));
            }

            var normalizedEmployeeNumber = employeeNumber.Trim();

            return await (
                from employee in _appDbContext.Employees.AsNoTracking()
                join store in _appDbContext.Stores.AsNoTracking()
                    on employee.StoreNumber equals store.StoreNumber
                where employee.EmployeeNumber == normalizedEmployeeNumber
                      && store.Id == storeId
                select employee).SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);

            var normalizedEmployeeNumber = employeeNumber.Trim();

            return await _appDbContext.Employees
                .AsNoTracking()
                .SingleOrDefaultAsync(employee => employee.EmployeeNumber == normalizedEmployeeNumber, cancellationToken);
        }

        public async Task<IReadOnlyList<Employee>>GetByEmployeeNumbersAsync(IReadOnlyCollection<string> employeeNumbers, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(employeeNumbers);

            if (employeeNumbers.Count == 0)
            {
                return [];
            }

            var normalizedEmployeeNumbers = employeeNumbers
                .Where(employeeNumber => !string.IsNullOrWhiteSpace(employeeNumber))
                .Select(employeeNumber => employeeNumber.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (normalizedEmployeeNumbers.Length == 0)
            {
                return [];
            }

            return await _appDbContext.Employees
                .AsNoTracking()
                .Where(employee => normalizedEmployeeNumbers.Contains(employee.EmployeeNumber))
                .OrderBy(employee => employee.FullName)
                .ToListAsync(cancellationToken);
        }
    }
}