using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Application.Common.Interfaces.ServiceDesk;

/// <summary>
/// Provides persistence operations for ServiceDesk request note
/// synchronization outbox entries.
/// </summary>
/// <remarks>
/// Note creation operations are reserved atomically.
///
/// External ServiceDesk API calls must not be executed
/// inside database transactions.
///
/// Uncertain external results require reconciliation
/// and must not be automatically retried.
/// </remarks>
public interface IServiceDeskNoteOutboxRepository
{
    /// <summary>
    /// Adds a new request note outbox entry.
    /// </summary>
    /// <remarks>
    /// This method does not save changes.
    /// The caller must persist the entry within the
    /// corresponding service case transaction.
    /// </remarks>
    Task AddAsync(ServiceDeskNoteOutbox outbox, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves pending note outbox identifiers
    /// ordered by creation time.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a note outbox entry without tracking.
    /// </summary>
    Task<ServiceDeskNoteOutbox?> GetByIdAsync(Guid outboxId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically reserves a pending note creation operation.
    /// </summary>
    /// <remarks>
    /// Reservation succeeds only when:
    /// - the entry is Pending,
    /// - no processing operation has reserved the entry,
    /// - the associated ServiceDesk request exists,
    /// - request synchronization is Completed,
    /// - all earlier note operations for the same service case
    ///   have completed successfully.
    /// </remarks>
    Task<bool> TryBeginProcessingAsync(Guid outboxId, Guid operationId, DateTime startedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically completes a note creation operation
    /// after successful external synchronization.
    /// </summary>
    /// <remarks>
    /// Completion is allowed only for the processing operation
    /// that reserved the entry.
    /// </remarks>
    Task<bool> TryCompleteAsync(Guid outboxId, Guid operationId, string serviceDeskNoteId, DateTime completedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an uncertain note creation operation
    /// as requiring reconciliation.
    /// </summary>
    /// <remarks>
    /// Entries requiring reconciliation must not
    /// automatically return to Pending.
    /// </remarks>
    Task<bool> TryRequireReconciliationAsync(Guid outboxId, Guid operationId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a definitively failed note creation operation.
    /// </summary>
    Task<bool> TryFailAsync(Guid outboxId, Guid operationId, string reason, DateTime failedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves note operations that have remained
    /// in Processing beyond the configured timeout.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetStaleProcessingIdsAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically marks a stale processing operation
    /// as requiring reconciliation.
    /// </summary>
    /// <remarks>
    /// The original ProcessingOperationId must be preserved.
    /// Stale operations must never automatically return to Pending.
    /// </remarks>
    Task<bool> TryMarkStaleProcessingForReconciliationAsync(Guid outboxId, DateTime olderThanUtc, string reason, CancellationToken cancellationToken = default);
}