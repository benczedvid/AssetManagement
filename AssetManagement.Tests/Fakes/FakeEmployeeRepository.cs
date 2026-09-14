using AssetManagement.Application.Common.Interfaces.Employees;
using AssetManagement.Domain.Entities.Employees;

namespace AssetManagement.Tests.Fakes
{
    public sealed class FakeEmployeeRepository : IEmployeeRepository
    {
        private readonly List<Employee> _employees = [];
        public IReadOnlyList<Employee> Employees => _employees.AsReadOnly();
        public int GetAllCallCount { get; private set; }
        public int GetAllByStoreIdCallCount { get; private set; }
        public int GetByEmployeeNumberAndStoreIdCallCount { get; private set; }
        public int GetByEmployeeNumberCallCount { get; private set; }
        public int GetByEmployeeNumbersCallCount { get; private set; }
        public Guid? LastRequestedStoreId { get; private set; }
        public string? LastRequestedEmployeeNumber { get; private set; }
        public IReadOnlyCollection<string>LastRequestedEmployeeNumbers { get; private set; } = Array.Empty<string>();
        public CancellationToken LastGetAllCancellationToken { get; private set; }
        public CancellationToken LastGetAllByStoreIdCancellationToken { get; private set; }
        public CancellationToken LastGetByEmployeeNumberAndStoreCancellationToken { get; private set; }
        public CancellationToken LastGetByEmployeeNumberCancellationToken { get; private set; }
        public CancellationToken LastGetByEmployeeNumbersCancellationToken { get; private set; }

        public Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            GetAllCallCount++;
            LastGetAllCancellationToken = cancellationToken;

            IReadOnlyList<Employee> employees = _employees.ToList();

            return Task.FromResult(employees);
        }

        public Task<IReadOnlyList<Employee>> GetAllByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            GetAllByStoreIdCallCount++;
            LastRequestedStoreId = storeId;
            LastGetAllByStoreIdCancellationToken = cancellationToken;

            var storeNumber = storeId.ToString();

            IReadOnlyList<Employee> employees = _employees
                .Where(employee =>
                    string.Equals(
                        employee.StoreNumber,
                        storeNumber,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(employees);
        }

        public Task<Employee?>GetByEmployeeNumberAndStoreIdAsync(string employeeNumber, Guid storeId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);

            GetByEmployeeNumberAndStoreIdCallCount++;
            LastRequestedEmployeeNumber = employeeNumber;
            LastRequestedStoreId = storeId;

            LastGetByEmployeeNumberAndStoreCancellationToken = cancellationToken;

            var storeNumber = storeId.ToString();

            var employee = _employees.SingleOrDefault(
                currentEmployee =>
                    string.Equals(
                        currentEmployee.EmployeeNumber,
                        employeeNumber,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        currentEmployee.StoreNumber,
                        storeNumber,
                        StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(employee);
        }

        public Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);

            GetByEmployeeNumberCallCount++;
            LastRequestedEmployeeNumber = employeeNumber;
            LastGetByEmployeeNumberCancellationToken =  cancellationToken;

            var employee = _employees.SingleOrDefault(
                currentEmployee =>
                    string.Equals(
                        currentEmployee.EmployeeNumber,
                        employeeNumber,
                        StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(employee);
        }

        public Task<IReadOnlyList<Employee>>GetByEmployeeNumbersAsync(IReadOnlyCollection<string> employeeNumbers, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(employeeNumbers);

            GetByEmployeeNumbersCallCount++;
            LastGetByEmployeeNumbersCancellationToken = cancellationToken;
            LastRequestedEmployeeNumbers = employeeNumbers.ToArray();

            var requestedEmployeeNumbers = employeeNumbers.ToHashSet(StringComparer.OrdinalIgnoreCase);

            IReadOnlyList<Employee> employees = _employees
                .Where(employee =>
                    requestedEmployeeNumbers.Contains(
                        employee.EmployeeNumber))
                .ToList();

            return Task.FromResult(employees);
        }

        public void Seed(Employee employee)
        {
            ArgumentNullException.ThrowIfNull(employee);

            _employees.Add(employee);
        }

        public void Seed(IEnumerable<Employee> employees)
        {
            ArgumentNullException.ThrowIfNull(employees);

            foreach (var employee in employees)
            {
                Seed(employee);
            }
        }
    }
}