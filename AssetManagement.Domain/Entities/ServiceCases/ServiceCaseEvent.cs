namespace AssetManagement.Domain.Entities.ServiceCases;

public sealed class ServiceCaseEvent
{
    public Guid Id { get; private set; }
    public Guid ServiceCaseId { get; private set; }
    public ServiceCaseEventType Type { get; private set; }
    public ServiceCaseStatus? PreviousStatus { get; private set; }
    public ServiceCaseStatus NewStatus { get; private set; }
    public string? Comment { get; private set; }
    public Guid PerformedByUserId { get; private set; }
    public string PerformedByUserName { get; private set; } = null!;
    public DateTime OccurredAtUtc { get; private set; }

    private ServiceCaseEvent()
    {
    }

    private ServiceCaseEvent(
        Guid id,
        Guid serviceCaseId,
        ServiceCaseEventType type,
        ServiceCaseStatus? previousStatus,
        ServiceCaseStatus newStatus,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc)
    {
        Id = id;
        ServiceCaseId = serviceCaseId;
        Type = type;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Comment = comment;
        PerformedByUserId = performedByUserId;
        PerformedByUserName = performedByUserName;
        OccurredAtUtc = occurredAtUtc;
    }

    public static ServiceCaseEvent Create(
        Guid serviceCaseId,
        ServiceCaseEventType type,
        ServiceCaseStatus? previousStatus,
        ServiceCaseStatus newStatus,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc)
    {
        ValidateIdentifier(serviceCaseId, nameof(serviceCaseId),"Service case identifier cannot be empty.");
        ValidateEventType(type);
        ValidateStatus(previousStatus, nameof(previousStatus));
        ValidateStatus(newStatus, nameof(newStatus));
        ValidateStatusChange(type, previousStatus, newStatus);
        ValidateComment(comment);
        ValidateIdentifier(performedByUserId, nameof(performedByUserId),"Performed by user identifier cannot be empty.");
        ValidatePerformedByUserName(performedByUserName);
        ValidateOccurredAtUtc(occurredAtUtc);

        return new ServiceCaseEvent(
            id: Guid.NewGuid(),
            serviceCaseId: serviceCaseId,
            type: type,
            previousStatus: previousStatus,
            newStatus: newStatus,
            comment: NormalizeOptionalText(comment),
            performedByUserId: performedByUserId,
            performedByUserName: performedByUserName.Trim(),
            occurredAtUtc: occurredAtUtc);
    }

    private static void ValidateIdentifier(Guid identifier, string parameterName, string errorMessage)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(errorMessage, parameterName);
        }
    }

    private static void ValidateEventType(ServiceCaseEventType type)
    {
        if (type == ServiceCaseEventType.Unknown || !Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type,"A valid service case event type must be specified.");
        }
    }

    private static void ValidateStatus(ServiceCaseStatus? status, string parameterName)
    {
        if (!status.HasValue)
        {
            return;
        }

        if (status.Value == ServiceCaseStatus.Unknown || !Enum.IsDefined(status.Value))
        {
            throw new ArgumentOutOfRangeException(parameterName, status.Value,"A valid service case status must be specified.");
        }
    }

    private static void ValidateStatus(ServiceCaseStatus status, string parameterName)
    {
        if (status == ServiceCaseStatus.Unknown || !Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(parameterName, status, "A valid service case status must be specified.");
        }
    }

    private static void ValidateStatusChange(ServiceCaseEventType type, ServiceCaseStatus? previousStatus, ServiceCaseStatus newStatus)
    {
        if (type == ServiceCaseEventType.Created)
        {
            if (previousStatus.HasValue)
            {
                throw new ArgumentException("A service case creation event cannot have a previous status.", nameof(previousStatus));
            }

            return;
        }

        if (!previousStatus.HasValue)
        {
            throw new ArgumentException("A previous status is required for a service case event other than creation.", nameof(previousStatus));
        }

        if (previousStatus.Value == newStatus)
        {
            throw new ArgumentException("Previous status and new status cannot be the same.", nameof(newStatus));
        }
    }

    private static void ValidateComment(string? comment)
    {
        if (comment is not null && string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("Comment cannot be empty or whitespace.", nameof(comment));
        }

        if (comment?.Length > 2000)
        {
            throw new ArgumentException("Comment cannot exceed 2000 characters.", nameof(comment));
        }
    }

    private static void ValidatePerformedByUserName(string performedByUserName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(performedByUserName);

        if (performedByUserName.Length > 250)
        {
            throw new ArgumentException("Performed by user name cannot exceed 250 characters.", nameof(performedByUserName));
        }
    }

    private static void ValidateOccurredAtUtc(DateTime occurredAtUtc)
    {
        if (occurredAtUtc == default)
        {
            throw new ArgumentException("Occurred date cannot be empty.", nameof(occurredAtUtc));
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Occurred date must be in UTC.", nameof(occurredAtUtc));
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return value?.Trim();
    }
}