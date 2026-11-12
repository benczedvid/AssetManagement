namespace AssetManagement.Domain.Entities.ServiceDesk;

/// <summary>
/// Represents a durable outbox entry for creating a
/// ServiceDesk Plus request note associated with a service case event.
/// </summary>
/// <remarks>
/// Each intermediate service case event must have at most one
/// corresponding note outbox entry.
///
/// External ServiceDesk API calls are performed outside
/// the service case database transaction.
///
/// Uncertain external API results require reconciliation
/// and must never be automatically retried.
/// </remarks>
public sealed class ServiceDeskNoteOutbox
{
    private const int MaximumNoteIdLength = 100;
    private const int MaximumErrorLength = 1000;

    private ServiceDeskNoteOutbox()
    {
    }

    public Guid Id { get; private set; }
    public Guid ServiceCaseId { get; private set; }
    public Guid ServiceCaseEventId { get; private set; }
    public string? ServiceDeskNoteId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public ServiceDeskNoteOutboxStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessingStartedAtUtc { get; private set; }
    public Guid? ProcessingOperationId { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? Error { get; private set; }

    /// <summary>
    /// Creates a pending request note synchronization entry.
    /// </summary>
    public static ServiceDeskNoteOutbox Create(
        Guid serviceCaseId,
        Guid serviceCaseEventId,
        string description,
        DateTime createdAtUtc)
    {
        ValidateIdentifier(serviceCaseId, nameof(serviceCaseId));
        ValidateIdentifier(serviceCaseEventId, nameof(serviceCaseEventId));
        ValidateUtcTimestamp(createdAtUtc, nameof(createdAtUtc));

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("The ServiceDesk note description cannot be empty.", nameof(description));
        }

        var normalizedDescription = description.Trim();

        return new ServiceDeskNoteOutbox
        {
            Id = Guid.NewGuid(),
            ServiceCaseId = serviceCaseId,
            ServiceCaseEventId = serviceCaseEventId,
            Description = normalizedDescription,
            Status = ServiceDeskNoteOutboxStatus.Pending,
            CreatedAtUtc = createdAtUtc
        };
    }

    /// <summary>
    /// Reserves the entry for an external synchronization operation.
    /// </summary>
    public void BeginProcessing(Guid operationId, DateTime startedAtUtc)
    {
        ValidateIdentifier(operationId, nameof(operationId));
        ValidateUtcTimestamp(startedAtUtc, nameof(startedAtUtc));

        if (Status != ServiceDeskNoteOutboxStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending ServiceDesk note can begin processing.");
        }

        if (ProcessingOperationId.HasValue)
        {
            throw new InvalidOperationException("The ServiceDesk note already has a processing operation.");
        }

        Status = ServiceDeskNoteOutboxStatus.Processing;
        ProcessingOperationId = operationId;
        ProcessingStartedAtUtc = startedAtUtc;
        Error = null;
    }

    /// <summary>
    /// Marks the note as successfully created in ServiceDesk Plus.
    /// </summary>
    public void Complete(Guid operationId, string serviceDeskNoteId, DateTime completedAtUtc)
    {
        EnsureCurrentProcessingOperation(operationId);
        ValidateUtcTimestamp(completedAtUtc, nameof(completedAtUtc));

        if (string.IsNullOrWhiteSpace(serviceDeskNoteId))
        {
            throw new ArgumentException("The ServiceDesk note identifier cannot be empty.", nameof(serviceDeskNoteId));
        }

        var normalizedNoteId = serviceDeskNoteId.Trim();

        if (normalizedNoteId.Length > MaximumNoteIdLength)
        {
            throw new ArgumentException($"The ServiceDesk note identifier cannot exceed {MaximumNoteIdLength} characters.", nameof(serviceDeskNoteId));
        }

        ServiceDeskNoteId = normalizedNoteId;
        Status = ServiceDeskNoteOutboxStatus.Completed;
        ProcessedAtUtc = completedAtUtc;
        Error = null;
    }

    /// <summary>
    /// Marks an uncertain external synchronization operation
    /// as requiring reconciliation.
    /// </summary>
    public void RequireReconciliation(Guid operationId, string reason)
    {
        EnsureCurrentProcessingOperation(operationId);

        Status = ServiceDeskNoteOutboxStatus.RequiresReconciliation;
        Error = NormalizeError(reason);
    }

    /// <summary>
    /// Marks a synchronization operation as failed.
    /// </summary>
    public void Fail(Guid operationId, string reason, DateTime failedAtUtc)
    {
        EnsureCurrentProcessingOperation(operationId);
        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc));

        Status = ServiceDeskNoteOutboxStatus.Failed;
        ProcessedAtUtc = failedAtUtc;
        Error = NormalizeError(reason);
    }

    private void EnsureCurrentProcessingOperation(Guid operationId)
    {
        ValidateIdentifier(operationId, nameof(operationId));

        if (Status != ServiceDeskNoteOutboxStatus.Processing || ProcessingOperationId != operationId)
        {
            throw new InvalidOperationException("The ServiceDesk note is not reserved by the specified processing operation.");
        }
    }

    private static void ValidateIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }

    private static void ValidateUtcTimestamp(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must be in UTC.", parameterName);
        }
    }

    private static string NormalizeError(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("The error reason cannot be empty.", nameof(reason));
        }

        var normalizedReason = reason.Trim();

        return normalizedReason.Length > MaximumErrorLength ? normalizedReason[..MaximumErrorLength] : normalizedReason;
    }
}