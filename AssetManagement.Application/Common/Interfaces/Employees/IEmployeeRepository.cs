using AssetManagement.Domain.Entities.Employees;

namespace AssetManagement.Application.Common.Interfaces.Employees
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Employee>> GetAllByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Employee>> GetByEmployeeNumbersAsync(IReadOnlyCollection<string> employeeNumbers, CancellationToken cancellationToken = default);
        Task<Employee?> GetByEmployeeNumberAndStoreIdAsync(string employeeNumber, Guid storeId, CancellationToken cancellationToken = default);
    }
}