using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Domain.Entities.ServiceDesk;
using AssetManagement.Domain.Entities.ServiceCases;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Provides persistence operations for ServiceDesk request
/// closure outbox entries.
/// </summary>
/// <remarks>
/// Closure operations are reserved atomically.
///
/// A ServiceDesk request can only be closed when:
/// - the associated service case is Closed,
/// - the external ServiceDesk request exists,
/// - request synchronization is Completed.
///
/// External ServiceDesk API calls must not be executed
/// inside database transactions.
/// </remarks>
public sealed class ServiceDeskRequestClosureOutboxRepository : IServiceDeskRequestClosureOutboxRepository
{
    private const int MaximumBatchSize = 100;
    private const int MaximumErrorLength = 1000;

    private readonly AppDbContext _appDbContext;
    public ServiceDeskRequestClosureOutboxRepository(AppDbContext appDbContext)
    {
        ArgumentNullException.ThrowIfNull(appDbContext);

        _appDbContext = appDbContext;
    }

    /// <summary>
    /// Adds a new request closure outbox entry.
    /// </summary>
    /// <remarks>
    /// This method does not call SaveChangesAsync.
    /// The caller must persist the entry within the
    /// service case closure transaction.
    /// </remarks>
    public async Task AddAsync(ServiceDeskRequestClosureOutbox outbox, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outbox);

        await _appDbContext.ServiceDeskRequestClosureOutbox.AddAsync(outbox, cancellationToken);
    }

    /// <summary>
    /// Retrieves pending closure operation identifiers
    /// ordered by creation time.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        ValidateBatchSize(batchSize);

        return await _appDbContext.ServiceDeskRequestClosureOutbox
            .AsNoTracking()
            .Where(outbox => outbox.Status == ServiceDeskRequestClosureOutboxStatus.Pending)
            .OrderBy(outbox => outbox.CreatedAtUtc)
            .ThenBy(outbox => outbox.Id)
            .Select(outbox => outbox.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a request closure outbox entry without tracking.
    /// </summary>
    public async Task<ServiceDeskRequestClosureOutbox?> GetByIdAsync(Guid outboxId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));

        return await _appDbContext.ServiceDeskRequestClosureOutbox
            .AsNoTracking()
            .FirstOrDefaultAsync(outbox => outbox.Id == outboxId, cancellationToken);
    }

    /// <summary>
    /// Atomically reserves a ServiceDesk request closure operation.
    /// </summary>
    /// <remarks>
    /// Reservation succeeds only when:
    /// - the closure entry is Pending and unreserved,
    /// - the associated service case is Closed,
    /// - the external ServiceDesk request exists,
    /// - request synchronization is Completed,
    /// - every intermediate service case event has a completed note.
    ///
    /// External API calls are not executed inside database transactions.
    /// </remarks>
    public async Task<bool> TryBeginProcessingAsync(
        Guid outboxId,
        Guid operationId,
        DateTime startedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateUtcTimestamp(startedAtUtc, nameof(startedAtUtc));

        var affectedRows = await _appDbContext.ServiceDeskRequestClosureOutbox
            .Where(outbox =>
                outbox.Id == outboxId &&

                // Closure entry must be pending and unreserved.
                outbox.Status == ServiceDeskRequestClosureOutboxStatus.Pending &&
                outbox.ProcessingOperationId == null &&

                // The service case must be closed and synchronized.
                _appDbContext.ServiceCases.Any(serviceCase =>
                    serviceCase.Id == outbox.ServiceCaseId &&
                    serviceCase.Status == ServiceCaseStatus.Closed &&
                    serviceCase.ServiceDeskRequestId != null &&
                    serviceCase.ServiceDeskRequestId != "" &&
                    serviceCase.ServiceDeskSyncStatus == ServiceDeskSyncStatus.Completed) &&

                // No unfinished note synchronization may exist.
                !_appDbContext.ServiceDeskNoteOutbox.Any(note =>
                    note.ServiceCaseId == outbox.ServiceCaseId &&
                    note.Status != ServiceDeskNoteOutboxStatus.Completed) &&

                // Every intermediate event must have a completed note.
                !_appDbContext.ServiceCaseEvents.Any(serviceCaseEvent =>
                    serviceCaseEvent.ServiceCaseId == outbox.ServiceCaseId &&

                    serviceCaseEvent.Type != ServiceCaseEventType.Created &&
                    serviceCaseEvent.Type != ServiceCaseEventType.Closed &&

                    !_appDbContext.ServiceDeskNoteOutbox.Any(note =>
                        note.ServiceCaseEventId == serviceCaseEvent.Id &&
                        note.ServiceCaseId == outbox.ServiceCaseId &&
                        note.Status == ServiceDeskNoteOutboxStatus.Completed)))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, ServiceDeskRequestClosureOutboxStatus.Processing)
                    .SetProperty(outbox => outbox.ProcessingOperationId, operationId)
                    .SetProperty(outbox => outbox.ProcessingStartedAtUtc, startedAtUtc)
                    .SetProperty(outbox => outbox.Error, (string?)null), cancellationToken);

        return affectedRows == 1;
    }

    /// <summary>
    /// Atomically completes a request closure operation
    /// after successful external ServiceDesk request closure.
    /// </summary>
    /// <remarks>
    /// Completion is allowed only for the processing operation
    /// that reserved the closure entry.
    ///
    /// The external request must already have been closed.
    /// </remarks>
    public async Task<bool> TryCompleteAsync(
        Guid outboxId,
        Guid operationId,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateUtcTimestamp(completedAtUtc, nameof(completedAtUtc));

        var affectedRows = await _appDbContext.ServiceDeskRequestClosureOutbox
                .Where(outbox =>
                    outbox.Id == outboxId &&
                    outbox.ProcessingOperationId == operationId &&
                    outbox.Status == ServiceDeskRequestClosureOutboxStatus.Processing)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(outbox => outbox.Status, ServiceDeskRequestClosureOutboxStatus.Completed)
                        .SetProperty(outbox => outbox.ProcessedAtUtc, completedAtUtc)
                        .SetProperty(outbox => outbox.Error,  (string?)null), cancellationToken);

        return affectedRows == 1;
    }

    /// <summary>
    /// Marks an uncertain external request closure
    /// as requiring reconciliation.
    /// </summary>
    /// <remarks>
    /// Only the processing operation that reserved the entry
    /// can mark its result as uncertain.
    ///
    /// Entries requiring reconciliation must not be
    /// automatically returned to Pending.
    /// </remarks>
    public async Task<bool> TryRequireReconciliationAsync(
        Guid outboxId,
        Guid operationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));

        var normalizedReason = NormalizeError(reason);

        var affectedRows = await _appDbContext.ServiceDeskRequestClosureOutbox
                .Where(outbox =>
                    outbox.Id == outboxId &&
                    outbox.ProcessingOperationId == operationId &&
                    outbox.Status == ServiceDeskRequestClosureOutboxStatus.Processing)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(outbox => outbox.Status, ServiceDeskRequestClosureOutboxStatus.RequiresReconciliation)
                        .SetProperty(outbox => outbox.Error, normalizedReason), cancellationToken);

        return affectedRows == 1;
    }


    /// <summary>
    /// Validates that an identifier is not empty.
    /// </summary>
    private static void ValidateIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }

    /// <summary>
    /// Validates the requested batch size.
    /// </summary>
    private static void ValidateBatchSize(int batchSize)
    {
        if (batchSize <= 0 || batchSize > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), $"Batch size must be between 1 and {MaximumBatchSize}.");
        }
    }

    /// <summary>
    /// Validates that a timestamp represents UTC.
    /// </summary>
    private static void ValidateUtcTimestamp( DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must be in UTC.", parameterName);
        }
    }

    /// <summary>
    /// Normalizes and limits a stored error message.
    /// </summary>
    private static string NormalizeError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("The error message cannot be empty.", nameof(error));
        }

        var normalizedError = error.Trim();

        return normalizedError.Length > MaximumErrorLength
            ? normalizedError[..MaximumErrorLength]
            : normalizedError;
    }

    /// <summary>
    /// Atomically marks a stale request closure operation
    /// as requiring reconciliation.
    /// </summary>
    /// <remarks>
    /// The original ProcessingOperationId is preserved.
    ///
    /// Stale entries are never automatically returned to Pending.
    /// </remarks>
    public async Task<bool> TryMarkStaleProcessingForReconciliationAsync(
        Guid outboxId,
        DateTime olderThanUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateUtcTimestamp(olderThanUtc, nameof(olderThanUtc));

        var normalizedReason = NormalizeError(reason);

        var affectedRows = await _appDbContext.ServiceDeskRequestClosureOutbox
                .Where(outbox =>
                    outbox.Id == outboxId &&
                    outbox.Status == ServiceDeskRequestClosureOutboxStatus.Processing &&
                    outbox.ProcessingStartedAtUtc.HasValue &&
                    outbox.ProcessingStartedAtUtc.Value < olderThanUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(outbox => outbox.Status, ServiceDeskRequestClosureOutboxStatus.RequiresReconciliation)
                        .SetProperty(outbox => outbox.Error, normalizedReason), cancellationToken);

        return affectedRows == 1;
    }

    /// <summary>
    /// Completes an administratively verified ServiceDesk
    /// request closure reconciliation.
    /// </summary>
    /// <remarks>
    /// This method never calls the external ServiceDesk API.
    ///
    /// The external request must already have been verified
    /// as closed before invoking this operation.
    /// </remarks>
    public async Task<bool> TryCompleteReconciliationAsync(
        Guid outboxId,
        Guid operationId,
        Guid verifiedByUserId,
        string verifiedByUserName,
        DateTime verifiedAtUtc,
        string verificationComment,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateIdentifier(verifiedByUserId, nameof(verifiedByUserId));
        ValidateUtcTimestamp(verifiedAtUtc, nameof(verifiedAtUtc));

        if (string.IsNullOrWhiteSpace(verifiedByUserName))
        {
            throw new ArgumentException("The verifying user name cannot be empty.", nameof(verifiedByUserName));
        }

        var normalizedComment = NormalizeError(verificationComment);

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

        var outbox = await _appDbContext.ServiceDeskRequestClosureOutbox
            .AsNoTracking()
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskRequestClosureOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId)
            .Select(x => new
            {
                x.ServiceCaseId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (outbox is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var affectedRows = await _appDbContext.ServiceDeskRequestClosureOutbox
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskRequestClosureOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, ServiceDeskRequestClosureOutboxStatus.Completed)
                    .SetProperty(x => x.ProcessedAtUtc, verifiedAtUtc)
                    .SetProperty(x => x.Error, (string?)null), cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var audit = ServiceDeskReconciliationAudit.Create(
            operation: ServiceDeskReconciliationOperationType.RequestClosure,
            outboxId: outboxId,
            serviceCaseId: outbox.ServiceCaseId,
            processingOperationId: operationId,
            outcome: ServiceDeskReconciliationAuditOutcome.Completed,
            externalIdentifier: null,
            verifiedByUserId: verifiedByUserId,
            verifiedByUserName: verifiedByUserName,
            verifiedAtUtc: verifiedAtUtc,
            comment: normalizedComment);

        await _appDbContext.ServiceDeskReconciliationAudits.AddAsync(audit, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Marks an administratively verified ServiceDesk
    /// request closure reconciliation as failed.
    /// </summary>
    /// <remarks>
    /// This operation is allowed only after verifying
    /// that the external request was not closed.
    ///
    /// No external API call is performed.
    /// </remarks>
    public async Task<bool> TryFailReconciliationAsync(
        Guid outboxId,
        Guid operationId,
        Guid verifiedByUserId,
        string verifiedByUserName,
        DateTime verifiedAtUtc,
        string verificationComment,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateIdentifier(verifiedByUserId, nameof(verifiedByUserId));
        ValidateUtcTimestamp(verifiedAtUtc, nameof(verifiedAtUtc));

        if (string.IsNullOrWhiteSpace(verifiedByUserName))
        {
            throw new ArgumentException("The verifying user name cannot be empty.", nameof(verifiedByUserName));
        }

        var normalizedComment = NormalizeError(verificationComment);

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

        var outbox = await _appDbContext.ServiceDeskRequestClosureOutbox
            .AsNoTracking()
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskRequestClosureOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId)
            .Select(x => new
            {
                x.ServiceCaseId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (outbox is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var affectedRows = await _appDbContext.ServiceDeskRequestClosureOutbox
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskRequestClosureOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, ServiceDeskRequestClosureOutboxStatus.Failed)
                    .SetProperty(x => x.ProcessedAtUtc, verifiedAtUtc)
                    .SetProperty(x => x.Error, normalizedComment), cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var audit = ServiceDeskReconciliationAudit.Create(
            operation: ServiceDeskReconciliationOperationType.RequestClosure,
            outboxId: outboxId,
            serviceCaseId: outbox.ServiceCaseId,
            processingOperationId: operationId,
            outcome: ServiceDeskReconciliationAuditOutcome.Failed,
            externalIdentifier: null,
            verifiedByUserId: verifiedByUserId,
            verifiedByUserName: verifiedByUserName,
            verifiedAtUtc: verifiedAtUtc,
            comment: normalizedComment);

        await _appDbContext.ServiceDeskReconciliationAudits.AddAsync(audit, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Retrieves request closure operations that have remained
    /// in Processing beyond the configured timeout.
    /// </summary>
    /// <remarks>
    /// This operation does not modify outbox entries.
    /// </remarks>
    public async Task<IReadOnlyList<Guid>> GetStaleProcessingIdsAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        ValidateUtcTimestamp(olderThanUtc, nameof(olderThanUtc));

        ValidateBatchSize(batchSize);

        return await _appDbContext.ServiceDeskRequestClosureOutbox
            .AsNoTracking()
            .Where(outbox =>
                outbox.Status == ServiceDeskRequestClosureOutboxStatus.Processing &&
                outbox.ProcessingStartedAtUtc.HasValue &&
                outbox.ProcessingStartedAtUtc.Value < olderThanUtc)
            .OrderBy(outbox => outbox.ProcessingStartedAtUtc)
            .ThenBy(outbox => outbox.Id)
            .Select(outbox => outbox.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}