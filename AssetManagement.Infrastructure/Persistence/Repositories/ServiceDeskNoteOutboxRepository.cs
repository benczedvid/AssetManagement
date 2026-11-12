using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories;

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
/// and must never be automatically retried.
/// </remarks>
public sealed class ServiceDeskNoteOutboxRepository  : IServiceDeskNoteOutboxRepository
{
    private const int MaximumBatchSize = 100;
    private const int MaximumErrorLength = 1000;
    private const int MaximumNoteIdLength = 100;

    private readonly AppDbContext _appDbContext;

    public ServiceDeskNoteOutboxRepository(AppDbContext appDbContext)
    {
        ArgumentNullException.ThrowIfNull(appDbContext);

        _appDbContext = appDbContext;
    }

    /// <summary>
    /// Adds a new note outbox entry without saving changes.
    /// </summary>
    public async Task AddAsync(ServiceDeskNoteOutbox outbox, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outbox);

        await _appDbContext.ServiceDeskNoteOutbox.AddAsync(outbox, cancellationToken);
    }

    /// <summary>
    /// Retrieves pending note outbox identifiers ordered by creation time.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        ValidateBatchSize(batchSize);

        return await _appDbContext.ServiceDeskNoteOutbox
            .AsNoTracking()
            .Where(outbox => outbox.Status == ServiceDeskNoteOutboxStatus.Pending)
            .OrderBy(outbox => outbox.CreatedAtUtc)
            .ThenBy(outbox => outbox.Id)
            .Select(outbox => outbox.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a note outbox entry without tracking.
    /// </summary>
    public async Task<ServiceDeskNoteOutbox?> GetByIdAsync(Guid outboxId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));

        return await _appDbContext.ServiceDeskNoteOutbox
            .AsNoTracking()
            .FirstOrDefaultAsync(outbox => outbox.Id == outboxId, cancellationToken);
    }

    /// <summary>
    /// Atomically reserves a pending note creation operation.
    /// </summary>
    /// <remarks>
    /// Reservation succeeds only when:
    /// - the note is Pending and unreserved,
    /// - the associated ServiceDesk request exists,
    /// - request synchronization is Completed,
    /// - every earlier note for the same service case is Completed.
    ///
    /// Notes are ordered by CreatedAtUtc and Id.
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

        var affectedRows = await _appDbContext.ServiceDeskNoteOutbox
            .Where(outbox =>
                outbox.Id == outboxId &&

                // The note must be pending and unreserved.
                outbox.Status == ServiceDeskNoteOutboxStatus.Pending &&
                outbox.ProcessingOperationId == null &&

                // The external request must exist.
                _appDbContext.ServiceCases.Any(serviceCase =>
                    serviceCase.Id == outbox.ServiceCaseId &&
                    serviceCase.ServiceDeskRequestId != null &&
                    serviceCase.ServiceDeskRequestId != "" &&
                    serviceCase.ServiceDeskSyncStatus == ServiceDeskSyncStatus.Completed) &&

                // Every earlier note must have completed synchronization.
                !_appDbContext.ServiceDeskNoteOutbox.Any(previous =>
                    previous.ServiceCaseId == outbox.ServiceCaseId &&
                    (
                        previous.CreatedAtUtc < outbox.CreatedAtUtc ||
                        (
                            previous.CreatedAtUtc == outbox.CreatedAtUtc &&
                            previous.Id.CompareTo(outbox.Id) < 0
                        )
                    ) &&
                    previous.Status != ServiceDeskNoteOutboxStatus.Completed))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, ServiceDeskNoteOutboxStatus.Processing)
                    .SetProperty(outbox => outbox.ProcessingOperationId, operationId)
                    .SetProperty(outbox => outbox.ProcessingStartedAtUtc, startedAtUtc)
                    .SetProperty(outbox => outbox.Error, (string?)null), cancellationToken);

        return affectedRows == 1;
    }

    /// <summary>
    /// Atomically completes a successfully synchronized note.
    /// </summary>
    public async Task<bool> TryCompleteAsync(
        Guid outboxId,
        Guid operationId,
        string serviceDeskNoteId,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateUtcTimestamp(completedAtUtc, nameof(completedAtUtc));

        var normalizedNoteId = NormalizeNoteId(serviceDeskNoteId);

        var affectedRows = await _appDbContext.ServiceDeskNoteOutbox
            .Where(outbox =>
                outbox.Id == outboxId &&
                outbox.Status == ServiceDeskNoteOutboxStatus.Processing &&
                outbox.ProcessingOperationId == operationId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, ServiceDeskNoteOutboxStatus.Completed)
                    .SetProperty(outbox => outbox.ServiceDeskNoteId, normalizedNoteId)
                    .SetProperty(outbox => outbox.ProcessedAtUtc, completedAtUtc)
                    .SetProperty(outbox => outbox.Error, (string?)null), cancellationToken);

        return affectedRows == 1;
    }

    /// <summary>
    /// Marks an uncertain external note creation
    /// as requiring reconciliation.
    /// </summary>
    public async Task<bool> TryRequireReconciliationAsync(
        Guid outboxId,
        Guid operationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));

        var normalizedReason = NormalizeError(reason);

        var affectedRows = await _appDbContext.ServiceDeskNoteOutbox
            .Where(outbox =>
                outbox.Id == outboxId &&
                outbox.Status == ServiceDeskNoteOutboxStatus.Processing &&
                outbox.ProcessingOperationId == operationId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, ServiceDeskNoteOutboxStatus.RequiresReconciliation)
                    .SetProperty(outbox => outbox.Error, normalizedReason), cancellationToken);

        return affectedRows == 1;
    }

    /// <summary>
    /// Marks a definitively failed note creation operation.
    /// </summary>
    public async Task<bool> TryFailAsync(
        Guid outboxId,
        Guid operationId,
        string reason,
        DateTime failedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc));

        var normalizedReason = NormalizeError(reason);

        var affectedRows = await _appDbContext.ServiceDeskNoteOutbox
            .Where(outbox =>
                outbox.Id == outboxId &&
                outbox.Status == ServiceDeskNoteOutboxStatus.Processing &&
                outbox.ProcessingOperationId == operationId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, ServiceDeskNoteOutboxStatus.Failed)
                    .SetProperty(outbox => outbox.ProcessedAtUtc, failedAtUtc)
                    .SetProperty(outbox => outbox.Error, normalizedReason), cancellationToken);

        return affectedRows == 1;
    }

    /// <summary>
    /// Retrieves stale processing note identifiers.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetStaleProcessingIdsAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        ValidateUtcTimestamp(olderThanUtc, nameof(olderThanUtc));
        ValidateBatchSize(batchSize);

        return await _appDbContext.ServiceDeskNoteOutbox
            .AsNoTracking()
            .Where(outbox =>
                outbox.Status == ServiceDeskNoteOutboxStatus.Processing &&
                outbox.ProcessingStartedAtUtc.HasValue &&
                outbox.ProcessingStartedAtUtc.Value < olderThanUtc)
            .OrderBy(outbox => outbox.ProcessingStartedAtUtc)
            .ThenBy(outbox => outbox.Id)
            .Select(outbox => outbox.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Atomically marks a stale processing operation
    /// as requiring reconciliation.
    /// </summary>
    public async Task<bool> TryMarkStaleProcessingForReconciliationAsync(
        Guid outboxId,
        DateTime olderThanUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateUtcTimestamp(olderThanUtc, nameof(olderThanUtc));

        var normalizedReason = NormalizeError(reason);

        var affectedRows = await _appDbContext.ServiceDeskNoteOutbox
            .Where(outbox =>
                outbox.Id == outboxId &&
                outbox.Status == ServiceDeskNoteOutboxStatus.Processing &&
                outbox.ProcessingStartedAtUtc.HasValue &&
                outbox.ProcessingStartedAtUtc.Value < olderThanUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, ServiceDeskNoteOutboxStatus.RequiresReconciliation)
                    .SetProperty(outbox => outbox.Error, normalizedReason), cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryCompleteReconciliationAsync(
        Guid outboxId,
        Guid operationId,
        string serviceDeskNoteId,
        Guid verifiedByUserId,
        string verifiedByUserName,
        DateTime verifiedAtUtc,
        string comment,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateIdentifier(verifiedByUserId, nameof(verifiedByUserId));
        ValidateUtcTimestamp(verifiedAtUtc, nameof(verifiedAtUtc));
        ArgumentException.ThrowIfNullOrWhiteSpace(verifiedByUserName);

        var normalizedNoteId = NormalizeNoteId(serviceDeskNoteId);
        var normalizedComment = NormalizeError(comment);

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

        var outbox = await _appDbContext.ServiceDeskNoteOutbox
            .AsNoTracking()
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskNoteOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId &&
                x.ServiceDeskNoteId == null)
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

        var audit = ServiceDeskReconciliationAudit.Create(
            operation: ServiceDeskReconciliationOperationType.NoteCreation,
            outboxId: outboxId,
            serviceCaseId: outbox.ServiceCaseId,
            processingOperationId: operationId,
            outcome: ServiceDeskReconciliationAuditOutcome.Completed,
            externalIdentifier: normalizedNoteId,
            verifiedByUserId: verifiedByUserId,
            verifiedByUserName: verifiedByUserName,
            verifiedAtUtc: verifiedAtUtc,
            comment: normalizedComment);

        var affectedRows = await _appDbContext.ServiceDeskNoteOutbox
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskNoteOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId &&
                x.ServiceDeskNoteId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, ServiceDeskNoteOutboxStatus.Completed)
                    .SetProperty(x => x.ServiceDeskNoteId, normalizedNoteId)
                    .SetProperty(x => x.ProcessedAtUtc, verifiedAtUtc)
                    .SetProperty(x => x.Error, (string?)null), cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await _appDbContext.ServiceDeskReconciliationAudits.AddAsync(audit, cancellationToken);

        await _appDbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> TryFailReconciliationAsync(
        Guid outboxId,
        Guid operationId,
        Guid verifiedByUserId,
        string verifiedByUserName,
        DateTime verifiedAtUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateIdentifier(verifiedByUserId, nameof(verifiedByUserId));
        ValidateUtcTimestamp(verifiedAtUtc, nameof(verifiedAtUtc));
        ArgumentException.ThrowIfNullOrWhiteSpace(verifiedByUserName);

        var normalizedReason = NormalizeError(reason);

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

        var outbox = await _appDbContext.ServiceDeskNoteOutbox
            .AsNoTracking()
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskNoteOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId &&
                x.ServiceDeskNoteId == null)
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

        var audit = ServiceDeskReconciliationAudit.Create(
            operation: ServiceDeskReconciliationOperationType.NoteCreation,
            outboxId: outboxId,
            serviceCaseId: outbox.ServiceCaseId,
            processingOperationId: operationId,
            outcome: ServiceDeskReconciliationAuditOutcome.Failed,
            externalIdentifier: null,
            verifiedByUserId: verifiedByUserId,
            verifiedByUserName: verifiedByUserName,
            verifiedAtUtc: verifiedAtUtc,
            comment: normalizedReason);

        var affectedRows = await _appDbContext.ServiceDeskNoteOutbox
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskNoteOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId &&
                x.ServiceDeskNoteId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, ServiceDeskNoteOutboxStatus.Failed)
                    .SetProperty(x => x.ProcessedAtUtc, verifiedAtUtc)
                    .SetProperty(x => x.Error, normalizedReason),
                cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await _appDbContext.ServiceDeskReconciliationAudits.AddAsync(audit, cancellationToken);

        await _appDbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> TryRequestRetryAsync(
        Guid outboxId,
        Guid operationId,
        Guid requestedByUserId,
        string requestedByUserName,
        DateTime requestedAtUtc,
        string comment,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateIdentifier(requestedByUserId, nameof(requestedByUserId));
        ValidateUtcTimestamp(requestedAtUtc, nameof(requestedAtUtc));

        ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(comment);

        var normalizedComment = comment.Trim();

        if (normalizedComment.Length > MaximumErrorLength)
        {
            throw new ArgumentException($"The retry comment cannot exceed {MaximumErrorLength} characters.", nameof(comment));
        }

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

        var eligibleOutbox = _appDbContext.ServiceDeskNoteOutbox
            .Where(outbox =>
                outbox.Id == outboxId &&
                outbox.Status == ServiceDeskNoteOutboxStatus.Failed &&
                outbox.ProcessingOperationId == operationId &&
                outbox.ServiceDeskNoteId == null &&
                _appDbContext.ServiceDeskReconciliationAudits.Any(audit =>
                    audit.Operation == ServiceDeskReconciliationOperationType.NoteCreation &&
                    audit.OutboxId == outbox.Id &&
                    audit.ServiceCaseId == outbox.ServiceCaseId &&
                    audit.ProcessingOperationId == operationId &&
                    audit.Outcome == ServiceDeskReconciliationAuditOutcome.Failed &&
                    audit.ExternalIdentifier == null));

        var entry = await eligibleOutbox
            .AsNoTracking()
            .Select(outbox => new
            {
                outbox.ServiceCaseId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var retryAudit = ServiceDeskReconciliationAudit.Create(
            operation: ServiceDeskReconciliationOperationType.NoteCreation,
            outboxId: outboxId,
            serviceCaseId: entry.ServiceCaseId,
            processingOperationId: operationId,
            outcome: ServiceDeskReconciliationAuditOutcome.RetryRequested,
            externalIdentifier: null,
            verifiedByUserId: requestedByUserId,
            verifiedByUserName: requestedByUserName,
            verifiedAtUtc: requestedAtUtc,
            comment: normalizedComment);

        var affectedRows = await eligibleOutbox
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, ServiceDeskNoteOutboxStatus.Pending)
                    .SetProperty(outbox => outbox.ProcessingOperationId, (Guid?)null)
                    .SetProperty(outbox => outbox.ProcessingStartedAtUtc, (DateTime?)null)
                    .SetProperty(outbox => outbox.ProcessedAtUtc, (DateTime?)null)
                    .SetProperty(outbox => outbox.Error, (string?)null), cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await _appDbContext.ServiceDeskReconciliationAudits.AddAsync(retryAudit, cancellationToken);

        await _appDbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private static void ValidateIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }

    private static void ValidateBatchSize(int batchSize)
    {
        if (batchSize <= 0 || batchSize > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), $"Batch size must be between 1 and {MaximumBatchSize}.");
        }
    }

    private static void ValidateUtcTimestamp(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must be in UTC.", parameterName);
        }
    }

    private static string NormalizeNoteId(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
        {
            throw new ArgumentException("The ServiceDesk note identifier cannot be empty.", nameof(noteId));
        }

        var normalizedNoteId = noteId.Trim();

        if (normalizedNoteId.Length > MaximumNoteIdLength)
        {
            throw new ArgumentException($"The ServiceDesk note identifier cannot exceed {MaximumNoteIdLength} characters.",
                nameof(noteId));
        }

        return normalizedNoteId;
    }

    private static string NormalizeError(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("The error reason cannot be empty.", nameof(reason));
        }

        var normalizedReason = reason.Trim();

        return normalizedReason.Length > MaximumErrorLength
            ? normalizedReason[..MaximumErrorLength]
            : normalizedReason;
    }
}