namespace AssetManagement.Domain.Entities.ServiceDesk;

/// <summary>
/// Represents a pending request to close an external
/// ServiceDesk Plus request after a service case is closed.
/// </summary>
/// <remarks>
/// The closure request is created within the same database
/// transaction as the service case closure.
///
/// External ServiceDesk requests must only be closed after
/// all related task synchronization operations have completed.
/// </remarks>
public sealed class ServiceDeskRequestClosureOutbox
{
    public Guid Id { get; private set; }
    public Guid ServiceCaseId { get; private set; }
    public Guid ServiceCaseEventId { get; private set; }
    public ServiceDeskRequestClosureOutboxStatus Status { get; private set; }
    public Guid? ProcessingOperationId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessingStartedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? Error { get; private set; }

    private ServiceDeskRequestClosureOutbox()
    {
    }

    /// <summary>
    /// Creates a new external request closure outbox entry.
    /// </summary>
    public static ServiceDeskRequestClosureOutbox Create(Guid serviceCaseId, Guid serviceCaseEventId, DateTime createdAtUtc)
    {
        if (serviceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
        }

        if (serviceCaseEventId == Guid.Empty)
        {
            throw new ArgumentException("The service case event identifier cannot be empty.", nameof(serviceCaseEventId));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The creation timestamp must be in UTC.", nameof(createdAtUtc));
        }

        return new ServiceDeskRequestClosureOutbox
        {
            Id = Guid.NewGuid(),
            ServiceCaseId = serviceCaseId,
            ServiceCaseEventId = serviceCaseEventId,
            Status = ServiceDeskRequestClosureOutboxStatus.Pending,
            ProcessingOperationId = null,
            CreatedAtUtc = createdAtUtc,
            ProcessingStartedAtUtc = null,
            ProcessedAtUtc = null,
            Error = null
        };
    }
}