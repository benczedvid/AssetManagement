using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Domain.Entities.Packages;

/// <summary>
/// Represents a package used to transport assets between stores
/// or between a store and headquarters.
/// </summary>
public sealed class Package
{
    private const int PackageNumberMaxLength = 50;
    private const int CommentMaxLength = 2000;
    private const int UserNameMaxLength = 250;
    private readonly List<PackageItem> _items = [];
    private readonly List<PackageEvent> _events = [];

    /// <summary>
    /// Gets the unique identifier of the package.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique business identifier of the package.
    /// </summary>
    public string PackageNumber { get; private set; } = null!;

    /// <summary>
    /// Gets the identifier of the source store.
    /// </summary>
    public Guid SourceStoreId { get; private set; }

    /// <summary>
    /// Gets the store from which the package is dispatched.
    /// </summary>
    public Store SourceStore { get; private set; } = null!;

    /// <summary>
    /// Gets the identifier of the destination store.
    /// </summary>
    public Guid DestinationStoreId { get; private set; }

    /// <summary>
    /// Gets the store at which the package is received.
    /// </summary>
    public Store DestinationStore { get; private set; } = null!;

    /// <summary>
    /// Gets the current lifecycle status of the package.
    /// </summary>
    public PackageStatus Status { get; private set; }

    /// <summary>
    /// Gets the identifier of the user assigned to deliver the package.
    /// </summary>
    public Guid? CourierUserId { get; private set; }
    /// <summary>
    /// Gets the application user assigned to deliver the package.
    /// </summary>
    public ApplicationUser? CourierUser { get; private set; }

    /// <summary>
    /// Gets the optional package comment.
    /// </summary>
    public string? Comment { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the package was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who created the package.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the package was dispatched.
    /// </summary>
    public DateTime? DispatchedAtUtc { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who dispatched the package.
    /// </summary>
    public Guid? DispatchedByUserId { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the package was received.
    /// </summary>
    public DateTime? ReceivedAtUtc { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who received the package.
    /// </summary>
    public Guid? ReceivedByUserId { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the package was cancelled.
    /// </summary>
    public DateTime? CancelledAtUtc { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who cancelled the package.
    /// </summary>
    public Guid? CancelledByUserId { get; private set; }

    /// <summary>
    /// Gets the assets included in the package.
    /// </summary>
    public IReadOnlyCollection<PackageItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Gets the events recorded during the package lifecycle.
    /// </summary>
    public IReadOnlyCollection<PackageEvent> Events => _events.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="Package"/> class.
    /// This constructor is required by Entity Framework Core.
    /// </summary>
    private Package()
    {
    }

    private Package(
        Guid id,
        string packageNumber,
        Guid sourceStoreId,
        Guid destinationStoreId,
        Guid? courierUserId,
        string? comment,
        DateTime createdAtUtc,
        Guid createdByUserId)
    {
        Id = id;
        PackageNumber = packageNumber;
        SourceStoreId = sourceStoreId;
        DestinationStoreId = destinationStoreId;
        Status = PackageStatus.Created;
        CourierUserId = courierUserId;
        Comment = comment;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    /// <summary>
    /// Creates a new package.
    /// </summary>
    /// <param name="packageNumber">
    /// The unique business identifier generated for the package.
    /// </param>
    /// <param name="sourceStoreId">
    /// The identifier of the source store.
    /// </param>
    /// <param name="destinationStoreId">
    /// The identifier of the destination store.
    /// </param>
    /// <param name="courierUserId">
    /// The optional identifier of the user assigned to deliver the package.
    /// </param>
    /// <param name="comment">
    /// An optional comment describing the package.
    /// </param>
    /// <param name="createdAtUtc">
    /// The UTC date and time when the package is created.
    /// </param>
    /// <param name="createdByUserId">
    /// The identifier of the user creating the package.
    /// </param>
    /// <param name="createdByUserName">
    /// The display name of the user creating the package.
    /// </param>
    /// <returns>
    /// A newly created package in the <see cref="PackageStatus.Created"/> status.
    /// </returns>
    public static Package Create(
        string packageNumber,
        Guid sourceStoreId,
        Guid destinationStoreId,
        Guid? courierUserId,
        string? comment,
        DateTime createdAtUtc,
        Guid createdByUserId,
        string createdByUserName)
    {
        ValidatePackageNumber(packageNumber);
        ValidateIdentifier(sourceStoreId, nameof(sourceStoreId), "Source store identifier cannot be empty.");
        ValidateIdentifier(destinationStoreId, nameof(destinationStoreId), "Destination store identifier cannot be empty.");

        if (sourceStoreId == destinationStoreId)
        {
            throw new ArgumentException("Source and destination stores must be different.", nameof(destinationStoreId));
        }

        ValidateOptionalIdentifier(courierUserId, nameof(courierUserId), "Courier user identifier cannot be empty.");

        ValidateComment(comment);
        ValidateUtcDate(createdAtUtc, nameof(createdAtUtc));

        ValidateIdentifier(createdByUserId, nameof(createdByUserId), "Created by user identifier cannot be empty.");

        ValidateUserName(createdByUserName, nameof(createdByUserName));

        var package = new Package(
            id: Guid.NewGuid(),
            packageNumber: packageNumber.Trim(),
            sourceStoreId: sourceStoreId,
            destinationStoreId: destinationStoreId,
            courierUserId: courierUserId,
            comment: NormalizeOptionalText(comment),
            createdAtUtc: createdAtUtc,
            createdByUserId: createdByUserId);

        package.AddEvent(
            PackageEventType.Created,
            previousStatus: null,
            newStatus: PackageStatus.Created,
            relatedAssetId: null,
            relatedUserId: null,
            comment: comment,
            performedByUserId: createdByUserId,
            performedByUserName: createdByUserName,
            occurredAtUtc: createdAtUtc);

        if (courierUserId.HasValue)
        {
            package.AddEvent(
                PackageEventType.CourierAssigned,
                previousStatus: PackageStatus.Created,
                newStatus: PackageStatus.Created,
                relatedAssetId: null,
                relatedUserId: courierUserId,
                comment: null,
                performedByUserId: createdByUserId,
                performedByUserName: createdByUserName,
                occurredAtUtc: createdAtUtc);
        }

        return package;
    }

    /// <summary>
    /// Adds an asset to the package.
    /// </summary>
    public void AddAsset(
        Guid assetId,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc)
    {
        EnsureCreatedStatus("Assets can only be added to a created package.");
        ValidateIdentifier(assetId, nameof(assetId), "Asset identifier cannot be empty.");
        ValidateOperationUser(performedByUserId, performedByUserName);
        ValidateUtcDate(occurredAtUtc, nameof(occurredAtUtc));

        if (_items.Any(item => item.AssetId == assetId))
        {
            throw new InvalidOperationException("The asset is already included in the package.");
        }

        var item = PackageItem.Create(
            packageId: Id,
            assetId: assetId,
            addedAtUtc: occurredAtUtc,
            addedByUserId: performedByUserId);

        _items.Add(item);

        AddEvent(
            PackageEventType.AssetAdded,
            previousStatus: Status,
            newStatus: Status,
            relatedAssetId: assetId,
            relatedUserId: null,
            comment: null,
            performedByUserId: performedByUserId,
            performedByUserName: performedByUserName,
            occurredAtUtc: occurredAtUtc);
    }

    /// <summary>
    /// Removes an asset from the package before dispatch.
    /// </summary>
    public void RemoveAsset(
        Guid assetId,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc)
    {
        EnsureCreatedStatus("Assets can only be removed from a created package.");
        ValidateIdentifier(assetId, nameof(assetId), "Asset identifier cannot be empty.");
        ValidateOperationUser(performedByUserId, performedByUserName);
        ValidateUtcDate(occurredAtUtc, nameof(occurredAtUtc));

        var item = _items.SingleOrDefault(packageItem => packageItem.AssetId == assetId);

        if (item is null)
        {
            throw new InvalidOperationException("The asset is not included in the package.");
        }

        _items.Remove(item);

        AddEvent(
            PackageEventType.AssetRemoved,
            previousStatus: Status,
            newStatus: Status,
            relatedAssetId: assetId,
            relatedUserId: null,
            comment: null,
            performedByUserId: performedByUserId,
            performedByUserName: performedByUserName,
            occurredAtUtc: occurredAtUtc);
    }

    /// <summary>
    /// Assigns or changes the user responsible for delivering the package.
    /// </summary>
    public void AssignCourier(
        Guid courierUserId,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc)
    {
        EnsureCreatedStatus("A courier can only be assigned to a created package.");
        ValidateIdentifier(courierUserId, nameof(courierUserId), "Courier user identifier cannot be empty.");
        ValidateComment(comment);
        ValidateOperationUser(performedByUserId, performedByUserName);
        ValidateUtcDate(occurredAtUtc, nameof(occurredAtUtc));

        if (CourierUserId == courierUserId)
        {
            throw new InvalidOperationException("The selected user is already assigned as the package courier.");
        }

        var eventType = CourierUserId.HasValue
            ? PackageEventType.CourierChanged
            : PackageEventType.CourierAssigned;

        CourierUserId = courierUserId;

        AddEvent(
            eventType,
            previousStatus: Status,
            newStatus: Status,
            relatedAssetId: null,
            relatedUserId: courierUserId,
            comment: comment,
            performedByUserId: performedByUserId,
            performedByUserName: performedByUserName,
            occurredAtUtc: occurredAtUtc);
    }

    /// <summary>
    /// Removes the currently assigned courier from the package.
    /// </summary>
    public void RemoveCourier(
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc)
    {
        EnsureCreatedStatus("The courier can only be removed from a created package.");
        ValidateComment(comment);
        ValidateOperationUser(performedByUserId, performedByUserName);
        ValidateUtcDate(occurredAtUtc, nameof(occurredAtUtc));

        if (!CourierUserId.HasValue)
        {
            throw new InvalidOperationException("The package does not have an assigned courier.");
        }

        var removedCourierUserId = CourierUserId.Value;

        CourierUserId = null;

        AddEvent(
            PackageEventType.CourierRemoved,
            previousStatus: Status,
            newStatus: Status,
            relatedAssetId: null,
            relatedUserId: removedCourierUserId,
            comment: comment,
            performedByUserId: performedByUserId,
            performedByUserName: performedByUserName,
            occurredAtUtc: occurredAtUtc);
    }

    /// <summary>
    /// Marks the package as dispatched from its source store.
    /// </summary>
    public void Dispatch(
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime dispatchedAtUtc)
    {
        EnsureCreatedStatus("Only created packages can be dispatched.");

        if (_items.Count == 0)
        {
            throw new InvalidOperationException("A package must contain at least one asset before dispatch.");
        }

        ValidateComment(comment);
        ValidateOperationUser(performedByUserId, performedByUserName);
        ValidateUtcDate(dispatchedAtUtc, nameof(dispatchedAtUtc));

        if (dispatchedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException("Dispatch date cannot be earlier than the package creation date.", nameof(dispatchedAtUtc));
        }

        var previousStatus = Status;

        Status = PackageStatus.InTransit;
        DispatchedAtUtc = dispatchedAtUtc;
        DispatchedByUserId = performedByUserId;

        AddEvent(
            PackageEventType.Dispatched,
            previousStatus: previousStatus,
            newStatus: Status,
            relatedAssetId: null,
            relatedUserId: null,
            comment: comment,
            performedByUserId: performedByUserId,
            performedByUserName: performedByUserName,
            occurredAtUtc: dispatchedAtUtc);
    }

    /// <summary>
    /// Marks the package as received at its destination store.
    /// </summary>
    public void Receive(
        Guid receivingStoreId,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime receivedAtUtc)
    {
        if (Status != PackageStatus.InTransit)
        {
            throw new InvalidOperationException("Only packages in transit can be received.");
        }

        ValidateIdentifier(receivingStoreId, nameof(receivingStoreId), "Receiving store identifier cannot be empty.");

        if (receivingStoreId != DestinationStoreId)
        {
            throw new InvalidOperationException("The package can only be received at its destination store.");
        }

        ValidateComment(comment);
        ValidateOperationUser(performedByUserId, performedByUserName);
        ValidateUtcDate(receivedAtUtc, nameof(receivedAtUtc));

        if (!DispatchedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The package does not have a dispatch date.");
        }

        if (receivedAtUtc < DispatchedAtUtc.Value)
        {
            throw new ArgumentException("Received date cannot be earlier than the dispatch date.", nameof(receivedAtUtc));
        }

        var previousStatus = Status;

        Status = PackageStatus.Received;
        ReceivedAtUtc = receivedAtUtc;
        ReceivedByUserId = performedByUserId;

        AddEvent(
            PackageEventType.Received,
            previousStatus: previousStatus,
            newStatus: Status,
            relatedAssetId: null,
            relatedUserId: null,
            comment: comment,
            performedByUserId: performedByUserId,
            performedByUserName: performedByUserName,
            occurredAtUtc: receivedAtUtc);
    }

    /// <summary>
    /// Cancels the package before dispatch.
    /// </summary>
    public void Cancel(
        string comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime cancelledAtUtc)
    {
        EnsureCreatedStatus("Only created packages can be cancelled.");

        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("A cancellation comment is required.", nameof(comment));
        }

        ValidateComment(comment);
        ValidateOperationUser(performedByUserId, performedByUserName);
        ValidateUtcDate(cancelledAtUtc, nameof(cancelledAtUtc));

        if (cancelledAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException("Cancellation date cannot be earlier than the package creation date.", nameof(cancelledAtUtc));
        }

        var previousStatus = Status;

        Status = PackageStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
        CancelledByUserId = performedByUserId;

        AddEvent(
            PackageEventType.Cancelled,
            previousStatus: previousStatus,
            newStatus: Status,
            relatedAssetId: null,
            relatedUserId: null,
            comment: comment,
            performedByUserId: performedByUserId,
            performedByUserName: performedByUserName,
            occurredAtUtc: cancelledAtUtc);
    }

    private void AddEvent(
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
        var packageEvent = PackageEvent.Create(
            packageId: Id,
            type: type,
            previousStatus: previousStatus,
            newStatus: newStatus,
            relatedAssetId: relatedAssetId,
            relatedUserId: relatedUserId,
            comment: comment,
            performedByUserId: performedByUserId,
            performedByUserName: performedByUserName,
            occurredAtUtc: occurredAtUtc);

        _events.Add(packageEvent);
    }

    private void EnsureCreatedStatus(string errorMessage)
    {
        if (Status != PackageStatus.Created)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static void ValidateOperationUser(Guid performedByUserId, string performedByUserName)
    {
        ValidateIdentifier(performedByUserId, nameof(performedByUserId), "Performed by user identifier cannot be empty.");
        ValidateUserName(performedByUserName, nameof(performedByUserName));
    }

    private static void ValidatePackageNumber(string packageNumber)
    {
        if (string.IsNullOrWhiteSpace(packageNumber))
        {
            throw new ArgumentException("Package number cannot be empty or whitespace.", nameof(packageNumber));
        }

        if (packageNumber.Trim().Length > PackageNumberMaxLength)
        {
            throw new ArgumentException($"Package number cannot exceed {PackageNumberMaxLength} characters.", nameof(packageNumber));
        }
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName,
        string errorMessage)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(errorMessage, parameterName);
        }
    }

    private static void ValidateOptionalIdentifier(
        Guid? identifier,
        string parameterName,
        string errorMessage)
    {
        if (identifier.HasValue && identifier.Value == Guid.Empty)
        {
            throw new ArgumentException(errorMessage, parameterName);
        }
    }

    private static void ValidateComment(string? comment)
    {
        if (comment is not null && string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("Comment cannot be empty or whitespace.", nameof(comment));
        }

        if (comment?.Trim().Length > CommentMaxLength)
        {
            throw new ArgumentException($"Comment cannot exceed {CommentMaxLength} characters.", nameof(comment));
        }
    }

    private static void ValidateUserName(string userName, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("User name cannot be empty or whitespace.", parameterName);
        }

        if (userName.Trim().Length > UserNameMaxLength)
        {
            throw new ArgumentException($"User name cannot exceed {UserNameMaxLength} characters.", parameterName);
        }
    }

    private static void ValidateUtcDate(DateTime value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException("Date cannot be empty.", parameterName);
        }

        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Date must be in UTC.", parameterName);
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return value?.Trim();
    }
}