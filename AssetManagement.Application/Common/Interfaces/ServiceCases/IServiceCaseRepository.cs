using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Application.Common.Interfaces.ServiceCases
{
    public interface IServiceCaseRepository
    {
        Task AddAsync(ServiceCase serviceCase, CancellationToken cancellationToken = default);
        Task AddEventAsync(ServiceCaseEvent serviceCaseEvent, CancellationToken cancellationToken = default);
        Task<ServiceCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ServiceCase?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ServiceCase?> GetActiveByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);
        Task<bool> HasActiveServiceCaseForAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceCaseEvent>> GetEventsByServiceCaseIdAsync(Guid serviceCaseId, CancellationToken cancellationToken = default);
        Task<ServiceCase?> GetActiveForUpdateByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceCase>> GetListAsync(Guid? originStoreId,  CancellationToken cancellationToken = default);
        Task<bool> TryBeginServiceDeskSyncAsync(Guid serviceCaseId, Guid operationId, DateTime startedAtUtc, CancellationToken cancellationToken = default);
        Task<bool> TryCompleteServiceDeskSyncAsync(Guid serviceCaseId, Guid operationId, string serviceDeskRequestId, DateTime completedAtUtc, CancellationToken cancellationToken = default);
        Task<bool> TryFailServiceDeskSyncAsync(Guid serviceCaseId, Guid operationId, string error, CancellationToken cancellationToken = default);
        Task<bool> TryRequireServiceDeskReconciliationAsync(Guid serviceCaseId, Guid operationId, string reason, CancellationToken cancellationToken = default);
        Task<bool> TryCompleteServiceDeskReconciliationAsync(Guid serviceCaseId, Guid operationId, string serviceDeskRequestId, DateTime completedAtUtc, CancellationToken cancellationToken = default);
        Task<bool> TryFailServiceDeskReconciliationAsync(Guid serviceCaseId, Guid operationId, string reason, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);


    }
}