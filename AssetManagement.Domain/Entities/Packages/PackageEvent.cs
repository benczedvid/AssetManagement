namespace AssetManagement.Domain.Entities.Packages;

/// <summary>
/// Represents an immutable event recorded during the lifecycle of a package.
/// </summary>
public sealed class PackageEvent
{
    /// <summary>
    /// Gets the unique identifier of the package event.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the package associated with the event.
    /// </summary>
    public Guid PackageId { get; private set; }

    /// <summary>
    /// Gets the type of the package event.
    /// </summary>
    public PackageEventType Type { get; private set; }

    /// <summary>
    /// Gets the package status before the event occurred.
    /// A creation event does not have a previous status.
    /// </summary>
    public PackageStatus? PreviousStatus { get; private set; }

    /// <summary>
    /// Gets the package status after the event occurred.
    /// </summary>
    public PackageStatus NewStatus { get; private set; }

    /// <summary>
    /// Gets the optional comment associated with the event.
    /// </summary>
    public string? Comment { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who performed the operation.
    /// </summary>
    public Guid PerformedByUserId { get; private set; }

    /// <summary>
    /// Gets the stored display name of the user who performed the operation.
    /// </summary>
    public string PerformedByUserName { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC date and time when the event occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>
    /// Gets the optional identifier of the asset associated with the event.
    /// </summary>
    public Guid? RelatedAssetId { get; private set; }

    /// <summary>
    /// Gets the optional identifier of the user associated with the event,
    /// such as the assigned, changed or removed courier.
    /// </summary>
    public Guid? RelatedUserId { get; private set; }

    private PackageEvent()
    {
    }

    private PackageEvent(
        Guid id,
        Guid packageId,
        PackageEventType type,
        PackageStatus? previousStatus,
        PackageStatus newStatus,
        Guid? relatedAssetId,
        Guid? relatedUserId,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc)
    {
        Id = id;
        PackageId = packageId;
        Type = type;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        RelatedAssetId = relatedAssetId;
        RelatedUserId = relatedUserId;
        Comment = comment;
        PerformedByUserId = performedByUserId;
        PerformedByUserName = performedByUserName;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>
    /// Creates a new package event.
    /// </summary>
    /// <param name="packageId">
    /// The identifier of the package associated with the event.
    /// </param>
    /// <param name="type">
    /// The type of the package event.
    /// </param>
    /// <param name="previousStatus">
    /// The package status before the event occurred.
    /// This value must be null for a creation event.
    /// </param>
    /// <param name="newStatus">
    /// The package status after the event occurred.
    /// </param>
    /// <param name="relatedAssetId">
    /// The optional identifier of the asset associated with the event.
    /// This value is required for asset addition and removal events.
    /// </param>
    /// <param name="relatedUserId">
    /// The optional identifier of the user associated with the event.
    /// This value is required for courier assignment, change and removal events.
    /// </param>
    /// <param name="comment">
    /// An optional comment describing the operation.
    /// </param>
    /// <param name="performedByUserId">
    /// The identifier of the user who performed the operation.
    /// </param>
    /// <param name="performedByUserName">
    /// The display name of the user who performed the operation.
    /// </param>
    /// <param name="occurredAtUtc">
    /// The UTC date and time when the event occurred.
    /// </param>
    /// <returns>
    /// A newly created package event.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an identifier, user name, comment, related entity,
    /// status transition or occurrence date is invalid.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the event type or a package status is invalid.
    /// </exception>
    public static PackageEvent Create(
        Guid packageId,
        PackageEventType type,
        PackageStatus? previousStatus,
        PackageStatus newStatus,
        Guid? relatedAssetId,
        Guid? relatedUserId,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc)
    {
        ValidateIdentifier(packageId, nameof(packageId),  "Package identifier cannot be empty.");

        ValidateEventType(type);
        ValidateStatus(previousStatus, nameof(previousStatus));
        ValidateStatus(newStatus, nameof(newStatus));

        ValidateStatusChange(type, previousStatus, newStatus);
        ValidateRelatedEntities(type, relatedAssetId, relatedUserId);

        ValidateComment(comment);

        ValidateIdentifier(performedByUserId, nameof(performedByUserId), "Performed by user identifier cannot be empty.");

        ValidatePerformedByUserName(performedByUserName);
        ValidateOccurredAtUtc(occurredAtUtc);

        return new PackageEvent(
            id: Guid.NewGuid(),
            packageId: packageId,
            type: type,
            previousStatus: previousStatus,
            newStatus: newStatus,
            relatedAssetId: relatedAssetId,
            relatedUserId: relatedUserId,
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

    private static void ValidateEventType(PackageEventType type)
    {
        if (type == PackageEventType.Unknown || !Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "A valid package event type must be specified.");
        }
    }

    private static void ValidateStatus(PackageStatus? status, string parameterName)
    {
        if (!status.HasValue)
        {
            return;
        }

        if (status.Value == PackageStatus.Unknown || !Enum.IsDefined(status.Value))
        {
            throw new ArgumentOutOfRangeException(parameterName, status.Value, "A valid package status must be specified.");
        }
    }

    private static void ValidateStatus(PackageStatus status, string parameterName)
    {
        if (status == PackageStatus.Unknown || !Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(parameterName, status, "A valid package status must be specified.");
        }
    }

    private static void ValidateStatusChange(PackageEventType type, PackageStatus? previousStatus, PackageStatus newStatus)
    {
        if (type == PackageEventType.Created)
        {
            if (previousStatus.HasValue)
            {
                throw new ArgumentException("A package creation event cannot have a previous status.", nameof(previousStatus));
            }

            if (newStatus != PackageStatus.Created)
            {
                throw new ArgumentException("A package creation event must result in the created status.", nameof(newStatus));
            }

            return;
        }

        if (!previousStatus.HasValue)
        {
            throw new ArgumentException("A previous status is required for a package event other than creation.", nameof(previousStatus));
        }

        if (ChangesPackageStatus(type))
        {
            if (previousStatus.Value == newStatus)
            {
                throw new ArgumentException("Previous status and new status cannot be the same for a status-changing package event.", nameof(newStatus));
            }

            ValidateLifecycleTransition(type, previousStatus.Value, newStatus);

            return;
        }

        if (previousStatus.Value != newStatus)
        {
            throw new ArgumentException("This package event type cannot change the package status.",  nameof(newStatus));
        }
    }

    private static bool ChangesPackageStatus(PackageEventType type)
    {
        return type is
            PackageEventType.Dispatched or
            PackageEventType.Received or
            PackageEventType.Cancelled;
    }

    private static void ValidateLifecycleTransition(
        PackageEventType type,
        PackageStatus previousStatus,
        PackageStatus newStatus)
    {
        var isValid = type switch
        {
            PackageEventType.Dispatched =>
                previousStatus == PackageStatus.Created && newStatus == PackageStatus.InTransit,

            PackageEventType.Received =>
                previousStatus == PackageStatus.InTransit && newStatus == PackageStatus.Received,

            PackageEventType.Cancelled =>
                previousStatus == PackageStatus.Created && newStatus == PackageStatus.Cancelled,

            _ => false
        };

        if (!isValid)
        {
            throw new ArgumentException(
                $"The package event '{type}' cannot change the package status " +
                $"from '{previousStatus}' to '{newStatus}'.",
                nameof(newStatus));
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
            throw new ArgumentException("Occurred date must be in UTC.",  nameof(occurredAtUtc));
        }
    }

    private static void ValidateRelatedEntities(PackageEventType type, Guid? relatedAssetId, Guid? relatedUserId)
    {
        ValidateOptionalIdentifier(relatedAssetId, nameof(relatedAssetId), "Related asset identifier cannot be empty.");
        ValidateOptionalIdentifier(relatedUserId, nameof(relatedUserId), "Related user identifier cannot be empty.");

        var requiresRelatedAsset = type is
            PackageEventType.AssetAdded or
            PackageEventType.AssetRemoved;

        if (requiresRelatedAsset && !relatedAssetId.HasValue)
        {
            throw new ArgumentException("A related asset identifier is required for an asset package event.", nameof(relatedAssetId));
        }

        var requiresRelatedUser = type is
            PackageEventType.CourierAssigned or
            PackageEventType.CourierChanged or
            PackageEventType.CourierRemoved;

        if (requiresRelatedUser && !relatedUserId.HasValue)
        {
            throw new ArgumentException("A related user identifier is required for a courier package event.", nameof(relatedUserId));
        }

        if (!requiresRelatedAsset && relatedAssetId.HasValue)
        {
            throw new ArgumentException("A related asset identifier is not allowed for this package event type.", nameof(relatedAssetId));
        }

        if (!requiresRelatedUser && relatedUserId.HasValue)
        {
            throw new ArgumentException("A related user identifier is not allowed for this package event type.", nameof(relatedUserId));
        }
    }

    private static void ValidateOptionalIdentifier(Guid? identifier, string parameterName, string errorMessage)
    {
        if (identifier.HasValue && identifier.Value == Guid.Empty)
        {
            throw new ArgumentException(errorMessage, parameterName);
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return value?.Trim();
    }
}