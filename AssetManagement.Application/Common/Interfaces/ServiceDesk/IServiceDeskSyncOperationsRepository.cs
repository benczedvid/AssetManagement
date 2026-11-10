using AssetManagement.Application.ServiceDesk.GetServiceDeskSyncOperations;

namespace AssetManagement.Application.Common.Interfaces.ServiceDesk;

public interface IServiceDeskSyncOperationsRepository
{
    Task<IReadOnlyList<ServiceDeskSyncOperationItem>> GetOperationsAsync(CancellationToken cancellationToken = default);
}