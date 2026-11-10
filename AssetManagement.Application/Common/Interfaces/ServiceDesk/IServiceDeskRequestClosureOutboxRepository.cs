using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Application.Common.Interfaces.ServiceDesk;

public interface IServiceDeskRequestClosureOutboxRepository
{
    Task AddAsync(ServiceDeskRequestClosureOutbox outbox, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<ServiceDeskRequestClosureOutbox?> GetByIdAsync(Guid outboxId, CancellationToken cancellationToken = default);
    Task<bool> TryBeginProcessingAsync(Guid outboxId, Guid operationId, DateTime startedAtUtc, CancellationToken cancellationToken = default);
    Task<bool> TryCompleteAsync(Guid outboxId, Guid operationId, DateTime completedAtUtc, CancellationToken cancellationToken = default);
    Task<bool> TryRequireReconciliationAsync(Guid outboxId, Guid operationId, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetStaleProcessingIdsAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default);
    Task<bool> TryMarkStaleProcessingForReconciliationAsync(Guid outboxId, DateTime olderThanUtc, string reason, CancellationToken cancellationToken = default);
    Task<bool> TryFailReconciliationAsync(Guid outboxId, Guid operationId, Guid verifiedByUserId, string verifiedByUserName, DateTime verifiedAtUtc, string verificationComment, CancellationToken cancellationToken = default);
    Task<bool> TryCompleteReconciliationAsync(Guid outboxId, Guid operationId, Guid verifiedByUserId, string verifiedByUserName, DateTime verifiedAtUtc, string verificationComment, CancellationToken cancellationToken = default);
}