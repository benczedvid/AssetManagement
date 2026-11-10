namespace AssetManagement.Domain.Entities.ServiceDesk;

/// <summary>
/// Represents a ServiceDesk Plus request creation operation.
/// </summary>
/// <remarks>
/// The outbox entry is created together with the service case
/// in the same database transaction.
///
/// The external ServiceDesk request is created asynchronously
/// after the transaction commits.
///
/// Uncertain creation results require reconciliation
/// and must not be automatically retried.
/// </remarks>
public sealed class ServiceDeskRequestOutbox
{
    private const int MaximumSubjectLength = 250;
    private const int MaximumDescriptionLength = 2000;

    public Guid Id { get; private set; }
    public Guid ServiceCaseId { get; private set; }
    public Guid ServiceCaseEventId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ServiceDeskRequestId { get; private set; }
    public ServiceDeskRequestOutboxStatus Status { get; private set; }
    public Guid? ProcessingOperationId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessingStartedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? Error { get; private set; }

    private ServiceDeskRequestOutbox()
    {
    }

    /// <summary>
    /// Creates a pending ServiceDesk request outbox entry.
    /// </summary>
    public static ServiceDeskRequestOutbox Create(
        Guid serviceCaseId,
        Guid serviceCaseEventId,
        string subject,
        string description,
        DateTime createdAtUtc)
    {
        if (serviceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
        }

        if (serviceCaseEventId == Guid.Empty)
        {
            throw new ArgumentException("The service case event identifier cannot be empty.", nameof(serviceCaseEventId));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("The ServiceDesk request subject cannot be empty.", nameof(subject));
        }

        var normalizedSubject = subject.Trim();

        if (normalizedSubject.Length > MaximumSubjectLength)
        {
            throw new ArgumentException($"The ServiceDesk request subject cannot exceed {MaximumSubjectLength} characters.",
                nameof(subject));
        }

        if (description is null)
        {
            throw new ArgumentNullException(nameof(description));
        }

        if (description.Length > MaximumDescriptionLength)
        {
            throw new ArgumentException($"The ServiceDesk request description cannot exceed {MaximumDescriptionLength} characters.",
                nameof(description));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The creation timestamp must be in UTC.", nameof(createdAtUtc));
        }

        return new ServiceDeskRequestOutbox
        {
            Id = Guid.NewGuid(),
            ServiceCaseId = serviceCaseId,
            ServiceCaseEventId = serviceCaseEventId,
            Subject = normalizedSubject,
            Description = description,
            ServiceDeskRequestId = null,
            Status = ServiceDeskRequestOutboxStatus.Pending,
            ProcessingOperationId = null,
            CreatedAtUtc = createdAtUtc,
            ProcessingStartedAtUtc = null,
            ProcessedAtUtc = null,
            Error = null
        };
    }
}