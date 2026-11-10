using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Provides persistence operations for automatic
/// ServiceDesk request creation.
/// </summary>
public sealed class ServiceDeskRequestOutboxRepository
    : IServiceDeskRequestOutboxRepository
{
    private const int MaximumBatchSize = 100;
    private const int MaximumRequestIdLength = 100;
    private const int MaximumErrorLength = 1000;

    private readonly AppDbContext _appDbContext;
    public ServiceDeskRequestOutboxRepository(AppDbContext appDbContext)
    {
        ArgumentNullException.ThrowIfNull(appDbContext);

        _appDbContext = appDbContext;
    }

    /// <summary>
    /// Adds a request creation outbox entry.
    /// </summary>
    public async Task AddAsync(ServiceDeskRequestOutbox outbox, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outbox);

        await _appDbContext.ServiceDeskRequestOutbox.AddAsync(outbox, cancellationToken);
    }

    /// <summary>
    /// Retrieves pending request creation operations.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        ValidateBatchSize(batchSize);

        return await _appDbContext.ServiceDeskRequestOutbox
            .AsNoTracking()
            .Where(x => x.Status == ServiceDeskRequestOutboxStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves an outbox entry without tracking.
    /// </summary>
    public async Task<ServiceDeskRequestOutbox?> GetByIdAsync(Guid outboxId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));

        return await _appDbContext.ServiceDeskRequestOutbox
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == outboxId, cancellationToken);
    }

    /// <summary>
    /// Atomically reserves a pending request creation operation.
    /// </summary>
    public async Task<bool> TryBeginProcessingAsync(
        Guid outboxId,
        Guid operationId,
        DateTime startedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateUtcTimestamp(startedAtUtc, nameof(startedAtUtc));

        var affectedRows = await _appDbContext.ServiceDeskRequestOutbox
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskRequestOutboxStatus.Pending &&
                x.ProcessingOperationId == null &&
                x.ServiceDeskRequestId == null &&
                _appDbContext.ServiceCases.Any(serviceCase =>
                    serviceCase.Id == x.ServiceCaseId &&
                    serviceCase.ServiceDeskRequestId == null &&
                    serviceCase.ServiceDeskSyncStatus == ServiceDeskSyncStatus.NotStarted))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, ServiceDeskRequestOutboxStatus.Processing)
                    .SetProperty(x => x.ProcessingOperationId, operationId)
                    .SetProperty(x => x.ProcessingStartedAtUtc, startedAtUtc)
                    .SetProperty(x => x.Error, (string?)null), cancellationToken);

        return affectedRows == 1;
    }

    /// <summary>
    /// Atomically completes the request creation operation
    /// and updates the associated service case.
    /// </summary>
    public async Task<bool> TryCompleteAsync(
        Guid outboxId,
        Guid operationId,
        string serviceDeskRequestId,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(outboxId, nameof(outboxId));
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateUtcTimestamp(completedAtUtc, nameof(completedAtUtc));

        if (string.IsNullOrWhiteSpace(serviceDeskRequestId))
        {
            throw new ArgumentException("The ServiceDesk request identifier cannot be empty.", nameof(serviceDeskRequestId));
        }

        var normalizedId = serviceDeskRequestId.Trim();

        if (normalizedId.Length > MaximumRequestIdLength)
        {
            throw new ArgumentException($"The ServiceDesk request identifier cannot exceed {MaximumRequestIdLength} characters.",
                nameof(serviceDeskRequestId));
        }

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

        var outbox = await _appDbContext.ServiceDeskRequestOutbox
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.Id == outboxId &&
                    x.ProcessingOperationId == operationId &&
                    x.Status == ServiceDeskRequestOutboxStatus.Processing &&
                    x.ServiceDeskRequestId == null,
                cancellationToken);

        if (outbox is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var affectedRows = await _appDbContext.ServiceDeskRequestOutbox
            .Where(x =>
                x.Id == outboxId &&
                x.ProcessingOperationId == operationId &&
                x.Status == ServiceDeskRequestOutboxStatus.Processing &&
                x.ServiceDeskRequestId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ServiceDeskRequestId, normalizedId)
                    .SetProperty(x => x.Status, ServiceDeskRequestOutboxStatus.Completed)
                    .SetProperty(x => x.ProcessedAtUtc, completedAtUtc)
                    .SetProperty(x => x.Error, (string?)null),
                cancellationToken);

        if (affectedRows != 1)
        {
            return false;
        }

        var serviceCaseRows = await _appDbContext.ServiceCases
            .Where(x =>
                x.Id == outbox.ServiceCaseId &&
                x.ServiceDeskRequestId == null &&
                x.ServiceDeskSyncStatus == ServiceDeskSyncStatus.NotStarted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ServiceDeskRequestId, normalizedId)
                    .SetProperty(x => x.ServiceDeskSyncStatus, ServiceDeskSyncStatus.Completed)
                    .SetProperty(x => x.ServiceDeskSyncCompletedAtUtc, completedAtUtc)
                    .SetProperty(x => x.ServiceDeskSyncError, (string?)null), cancellationToken);

        if (serviceCaseRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Marks an uncertain request creation result
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

        var affectedRows = await _appDbContext.ServiceDeskRequestOutbox
            .Where(x =>
                x.Id == outboxId &&
                x.ProcessingOperationId == operationId &&
                x.Status == ServiceDeskRequestOutboxStatus.Processing &&
                x.ServiceDeskRequestId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, ServiceDeskRequestOutboxStatus.RequiresReconciliation)
                    .SetProperty(x => x.Error, normalizedReason), cancellationToken);

        return affectedRows == 1;
    }

    /// <summary>
    /// Retrieves request creation operations that have remained
    /// in Processing beyond the configured timeout.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetStaleProcessingIdsAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        ValidateUtcTimestamp(olderThanUtc, nameof(olderThanUtc));

        ValidateBatchSize(batchSize);

        return await _appDbContext.ServiceDeskRequestOutbox
            .AsNoTracking()
            .Where(outbox =>
                outbox.Status == ServiceDeskRequestOutboxStatus.Processing &&
                outbox.ServiceDeskRequestId == null &&
                outbox.ProcessingStartedAtUtc.HasValue &&
                outbox.ProcessingStartedAtUtc.Value < olderThanUtc)
            .OrderBy(outbox => outbox.ProcessingStartedAtUtc)
            .ThenBy(outbox => outbox.Id)
            .Select(outbox => outbox.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Atomically marks a stale request creation operation
    /// as requiring reconciliation.
    /// </summary>
    /// <remarks>
    /// The original ProcessingOperationId is preserved.
    ///
    /// The entry is never automatically returned to Pending.
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

        var affectedRows = await _appDbContext.ServiceDeskRequestOutbox
            .Where(outbox =>
                outbox.Id == outboxId &&
                outbox.Status == ServiceDeskRequestOutboxStatus.Processing &&
                outbox.ServiceDeskRequestId == null &&
                outbox.ProcessingStartedAtUtc.HasValue &&
                outbox.ProcessingStartedAtUtc.Value < olderThanUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, ServiceDeskRequestOutboxStatus.RequiresReconciliation)
                    .SetProperty(outbox => outbox.Error, normalizedReason), cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryCompleteReconciliationAsync(
        Guid outboxId,
        Guid operationId,
        string serviceDeskRequestId,
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

        if (string.IsNullOrWhiteSpace(serviceDeskRequestId))
        {
            throw new ArgumentException("The verified ServiceDesk request identifier is required.", nameof(serviceDeskRequestId));
        }

        var normalizedRequestId = serviceDeskRequestId.Trim();

        if (normalizedRequestId.Length > MaximumRequestIdLength)
        {
            throw new ArgumentException($"The ServiceDesk request identifier cannot exceed {MaximumRequestIdLength} characters.",
                nameof(serviceDeskRequestId));
        }

        var normalizedComment = NormalizeError(comment);

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

        var outbox = await _appDbContext.ServiceDeskRequestOutbox
            .AsNoTracking()
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskRequestOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId &&
                x.ServiceDeskRequestId == null)
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

        var outboxRows = await _appDbContext.ServiceDeskRequestOutbox
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskRequestOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId &&
                x.ServiceDeskRequestId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ServiceDeskRequestId, normalizedRequestId)
                    .SetProperty(x => x.Status, ServiceDeskRequestOutboxStatus.Completed)
                    .SetProperty(x => x.ProcessedAtUtc, verifiedAtUtc)
                    .SetProperty(x => x.Error, (string?)null), cancellationToken);

        if (outboxRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var serviceCaseRows = await _appDbContext.ServiceCases
            .Where(x =>
                x.Id == outbox.ServiceCaseId &&
                x.ServiceDeskRequestId == null &&
                x.ServiceDeskSyncStatus !=  ServiceDeskSyncStatus.Completed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ServiceDeskRequestId, normalizedRequestId)
                    .SetProperty(x => x.ServiceDeskSyncStatus, ServiceDeskSyncStatus.Completed)
                    .SetProperty(x => x.ServiceDeskSyncCompletedAtUtc, verifiedAtUtc)
                    .SetProperty(x => x.ServiceDeskSyncError, (string?)null), cancellationToken);

        if (serviceCaseRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var audit = ServiceDeskReconciliationAudit.Create(
            operation: ServiceDeskReconciliationOperationType.RequestCreation,
            outboxId: outboxId,
            serviceCaseId: outbox.ServiceCaseId,
            processingOperationId: operationId,
            outcome: ServiceDeskReconciliationAuditOutcome.Completed,
            externalIdentifier: normalizedRequestId,
            verifiedByUserId: verifiedByUserId,
            verifiedByUserName: verifiedByUserName,
            verifiedAtUtc: verifiedAtUtc,
            comment: normalizedComment);

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

        var normalizedReason = NormalizeError(reason);

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

        var outbox = await _appDbContext.ServiceDeskRequestOutbox
            .AsNoTracking()
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskRequestOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId &&
                x.ServiceDeskRequestId == null)
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

        var outboxRows = await _appDbContext.ServiceDeskRequestOutbox
            .Where(x =>
                x.Id == outboxId &&
                x.Status == ServiceDeskRequestOutboxStatus.RequiresReconciliation &&
                x.ProcessingOperationId == operationId &&
                x.ServiceDeskRequestId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, ServiceDeskRequestOutboxStatus.Failed)
                    .SetProperty(x => x.ProcessedAtUtc, verifiedAtUtc)
                    .SetProperty(x => x.Error, normalizedReason), cancellationToken);

        if (outboxRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var serviceCaseRows = await _appDbContext.ServiceCases
            .Where(x =>
                x.Id == outbox.ServiceCaseId &&
                x.ServiceDeskRequestId == null &&
                x.ServiceDeskSyncStatus != ServiceDeskSyncStatus.Completed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ServiceDeskSyncStatus, ServiceDeskSyncStatus.Failed)
                    .SetProperty(x => x.ServiceDeskSyncCompletedAtUtc, (DateTime?)null)
                    .SetProperty(x => x.ServiceDeskSyncError, normalizedReason), cancellationToken);

        if (serviceCaseRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var audit = ServiceDeskReconciliationAudit.Create(
            operation: ServiceDeskReconciliationOperationType.RequestCreation,
            outboxId: outboxId,
            serviceCaseId: outbox.ServiceCaseId,
            processingOperationId: operationId,
            outcome: ServiceDeskReconciliationAuditOutcome.Failed,
            externalIdentifier: null,
            verifiedByUserId: verifiedByUserId,
            verifiedByUserName: verifiedByUserName,
            verifiedAtUtc: verifiedAtUtc,
            comment: normalizedReason);

        await _appDbContext.ServiceDeskReconciliationAudits.AddAsync(audit, cancellationToken);

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
            throw new ArgumentOutOfRangeException(nameof(batchSize),
                $"Batch size must be between 1 and {MaximumBatchSize}.");
        }
    }

    private static void ValidateUtcTimestamp(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must be in UTC.", parameterName);
        }
    }

    private static string NormalizeError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("The error message cannot be empty.", nameof(error));
        }

        var normalized = error.Trim();

        return normalized.Length > MaximumErrorLength
            ? normalized[..MaximumErrorLength]
            : normalized;
    }
}