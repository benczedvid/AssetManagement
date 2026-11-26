using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Tests.Fakes
{
    public sealed class FakeServiceCaseRepository : IServiceCaseRepository
    {
        private readonly List<ServiceCase> _serviceCases = [];
        private readonly List<ServiceCaseEvent> _events = [];
        public IReadOnlyList<ServiceCase> ServiceCases => _serviceCases.AsReadOnly();
        public IReadOnlyList<ServiceCaseEvent> Events => _events.AsReadOnly();
        public int AddCallCount { get; private set; }
        public int AddEventCallCount { get; private set; }
        public int GetByIdCallCount { get; private set; }
        public int GetForUpdateByIdCallCount { get; private set; }
        public int GetActiveByAssetIdCallCount { get; private set; }
        public int GetActiveForUpdateByAssetIdCallCount { get; private set; }
        public int HasActiveServiceCaseForAssetCallCount { get; private set; }
        public int GetEventsByServiceCaseIdCallCount { get; private set; }
        public int GetListCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public Guid? LastRequestedServiceCaseId { get; private set; }
        public Guid? LastRequestedAssetId { get; private set; }
        public Guid? LastRequestedOriginStoreId { get; private set; }
        public CancellationToken LastGetByIdCancellationToken { get; private set; }
        public CancellationToken LastGetForUpdateByIdCancellationToken { get; private set; }
        public CancellationToken LastAddEventCancellationToken { get; private set; }
        public CancellationToken LastGetListCancellationToken { get; private set; }
        public CancellationToken LastSaveChangesCancellationToken { get; private set; }

        /// <summary>
        /// Overrides the service case returned by <see cref="GetForUpdateByIdAsync"/>,
        /// simulating a change that happened between the preliminary lookup
        /// and the locked reload.
        /// </summary>
        public ServiceCase? ServiceCaseReturnedForUpdate { get; set; }

        public Task AddAsync(ServiceCase serviceCase, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(serviceCase);

            AddCallCount++;
            _serviceCases.Add(serviceCase);

            return Task.CompletedTask;
        }

        public Task AddEventAsync(ServiceCaseEvent serviceCaseEvent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(serviceCaseEvent);

            AddEventCallCount++;
            LastAddEventCancellationToken = cancellationToken;
            _events.Add(serviceCaseEvent);

            return Task.CompletedTask;
        }

        public Task<ServiceCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            LastRequestedServiceCaseId = id;
            LastGetByIdCancellationToken = cancellationToken;

            return Task.FromResult(_serviceCases.SingleOrDefault(serviceCase => serviceCase.Id == id));
        }

        public Task<ServiceCase?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetForUpdateByIdCallCount++;
            LastRequestedServiceCaseId = id;
            LastGetForUpdateByIdCancellationToken = cancellationToken;

            var serviceCase = ServiceCaseReturnedForUpdate
                ?? _serviceCases.SingleOrDefault(currentServiceCase => currentServiceCase.Id == id);

            return Task.FromResult(serviceCase);
        }

        public Task<ServiceCase?> GetActiveByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
        {
            GetActiveByAssetIdCallCount++;
            LastRequestedAssetId = assetId;

            return Task.FromResult(FindActiveByAssetId(assetId));
        }

        public Task<bool> HasActiveServiceCaseForAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
        {
            HasActiveServiceCaseForAssetCallCount++;
            LastRequestedAssetId = assetId;

            return Task.FromResult(FindActiveByAssetId(assetId) is not null);
        }

        public Task<IReadOnlyList<ServiceCaseEvent>> GetEventsByServiceCaseIdAsync(Guid serviceCaseId, CancellationToken cancellationToken = default)
        {
            GetEventsByServiceCaseIdCallCount++;
            LastRequestedServiceCaseId = serviceCaseId;

            IReadOnlyList<ServiceCaseEvent> events =
                [.. _events.Where(serviceCaseEvent => serviceCaseEvent.ServiceCaseId == serviceCaseId)];

            return Task.FromResult(events);
        }

        public Task<ServiceCase?> GetActiveForUpdateByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
        {
            GetActiveForUpdateByAssetIdCallCount++;
            LastRequestedAssetId = assetId;

            return Task.FromResult(FindActiveByAssetId(assetId));
        }

        public Task<IReadOnlyList<ServiceCase>> GetListAsync(Guid? originStoreId, CancellationToken cancellationToken = default)
        {
            GetListCallCount++;
            LastRequestedOriginStoreId = originStoreId;
            LastGetListCancellationToken = cancellationToken;

            IReadOnlyList<ServiceCase> serviceCases = originStoreId.HasValue
                ? [.. _serviceCases.Where(serviceCase => serviceCase.OriginStoreId == originStoreId)]
                : [.. _serviceCases];

            return Task.FromResult(serviceCases);
        }

        public Task<bool> TryBeginServiceDeskSyncAsync(Guid serviceCaseId, Guid operationId, DateTime startedAtUtc, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("ServiceDesk synchronization is not supported by this fake.");
        }

        public Task<bool> TryCompleteServiceDeskSyncAsync(Guid serviceCaseId, Guid operationId, string serviceDeskRequestId, DateTime completedAtUtc, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("ServiceDesk synchronization is not supported by this fake.");
        }

        public Task<bool> TryFailServiceDeskSyncAsync(Guid serviceCaseId, Guid operationId, string error, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("ServiceDesk synchronization is not supported by this fake.");
        }

        public Task<bool> TryRequireServiceDeskReconciliationAsync(Guid serviceCaseId, Guid operationId, string reason, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("ServiceDesk synchronization is not supported by this fake.");
        }

        public Task<bool> TryCompleteServiceDeskReconciliationAsync(Guid serviceCaseId, Guid operationId, string serviceDeskRequestId, DateTime completedAtUtc, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("ServiceDesk synchronization is not supported by this fake.");
        }

        public Task<bool> TryFailServiceDeskReconciliationAsync(Guid serviceCaseId, Guid operationId, string reason, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("ServiceDesk synchronization is not supported by this fake.");
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            LastSaveChangesCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }

        public void Seed(ServiceCase serviceCase)
        {
            ArgumentNullException.ThrowIfNull(serviceCase);

            _serviceCases.Add(serviceCase);
        }

        public void SeedEvent(ServiceCaseEvent serviceCaseEvent)
        {
            ArgumentNullException.ThrowIfNull(serviceCaseEvent);

            _events.Add(serviceCaseEvent);
        }

        private ServiceCase? FindActiveByAssetId(Guid assetId)
        {
            return _serviceCases.SingleOrDefault(serviceCase => serviceCase.AssetId == assetId && serviceCase.IsActive);
        }
    }
}
