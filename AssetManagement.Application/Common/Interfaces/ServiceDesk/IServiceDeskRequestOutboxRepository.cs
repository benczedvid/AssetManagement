using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Application.Common.Interfaces.ServiceDesk;

/// <summary>
/// Provides persistence operations for automatic
/// ServiceDesk request creation.
/// </summary>
public interface IServiceDeskRequestOutboxRepository
{
    Task AddAsync(ServiceDeskRequestOutbox outbox, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<ServiceDeskRequestOutbox?> GetByIdAsync(Guid outboxId, CancellationToken cancellationToken = default);
    Task<bool> TryBeginProcessingAsync(Guid outboxId, Guid operationId, DateTime startedAtUtc, CancellationToken cancellationToken = default);
    Task<bool> TryCompleteAsync(Guid outboxId, Guid operationId, string serviceDeskRequestId, DateTime completedAtUtc, CancellationToken cancellationToken = default);
    Task<bool> TryRequireReconciliationAsync(Guid outboxId, Guid operationId, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetStaleProcessingIdsAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default);
    Task<bool> TryMarkStaleProcessingForReconciliationAsync(Guid outboxId, DateTime olderThanUtc, string reason, CancellationToken cancellationToken = default);
    Task<bool> TryCompleteReconciliationAsync(
        Guid outboxId,
        Guid operationId,
        string serviceDeskRequestId,
        Guid verifiedByUserId,
        string verifiedByUserName,
        DateTime verifiedAtUtc,
        string comment,
        CancellationToken cancellationToken = default);

    Task<bool> TryFailReconciliationAsync(
        Guid outboxId,
        Guid operationId,
        Guid verifiedByUserId,
        string verifiedByUserName,
        DateTime verifiedAtUtc,
        string reason,
        CancellationToken cancellationToken = default);
}