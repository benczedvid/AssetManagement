using AssetManagement.Domain.Entities.ServiceCases;

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
        Task SaveChangesAsync(CancellationToken cancellationToken = default);


    }
}