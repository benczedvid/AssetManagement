using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.GetPackageById;

/// <summary>
/// Represents the detailed information of a package.
/// </summary>
/// <param name="Id">
/// The unique identifier of the package.
/// </param>
/// <param name="PackageNumber">
/// The generated business identifier of the package.
/// </param>
/// <param name="Status">
/// The current lifecycle status of the package.
/// </param>
/// <param name="SourceStoreId">
/// The identifier of the store from which the package is dispatched.
/// </param>
/// <param name="SourceStoreNumber">
/// The business identifier of the store from which the package is dispatched.
/// </param>
/// <param name="SourceStoreName">
/// The name of the store from which the package is dispatched.
/// </param>
/// <param name="DestinationStoreId">
/// The identifier of the store at which the package is received.
/// </param>
/// <param name="DestinationStoreNumber">
/// The business identifier of the store at which the package is received.
/// </param>
/// <param name="DestinationStoreName">
/// The name of the store at which the package is received.
/// </param>
/// <param name="CourierUserId">
/// The optional identifier of the user assigned to deliver the package.
/// </param>
/// <param name="CourierUserName">
/// The optional display name of the user assigned to deliver the package.
/// </param>
/// <param name="Comment">
/// The optional comment associated with the package.
/// </param>
/// <param name="CreatedAtUtc">
/// The UTC date and time when the package was created.
/// </param>
/// <param name="CreatedByUserId">
/// The identifier of the user who created the package.
/// </param>
/// <param name="DispatchedAtUtc">
/// The optional UTC date and time when the package was dispatched.
/// </param>
/// <param name="DispatchedByUserId">
/// The optional identifier of the user who dispatched the package.
/// </param>
/// <param name="ReceivedAtUtc">
/// The optional UTC date and time when the package was received.
/// </param>
/// <param name="ReceivedByUserId">
/// The optional identifier of the user who received the package.
/// </param>
/// <param name="CancelledAtUtc">
/// The optional UTC date and time when the package was cancelled.
/// </param>
/// <param name="CancelledByUserId">
/// The optional identifier of the user who cancelled the package.
/// </param>
/// <param name="Items">
/// The assets included in the package.
/// </param>
/// <param name="Events">
/// The events recorded during the package lifecycle.
/// </param>
public sealed record GetPackageByIdResponse(
    Guid Id,
    string PackageNumber,
    PackageStatus Status,
    Guid SourceStoreId,
    string SourceStoreNumber,
    string SourceStoreName,
    Guid DestinationStoreId,
    string DestinationStoreNumber,
    string DestinationStoreName,
    Guid? CourierUserId,
    string? CourierUserName,
    string? Comment,
    DateTime CreatedAtUtc,
    Guid CreatedByUserId,
    DateTime? DispatchedAtUtc,
    Guid? DispatchedByUserId,
    DateTime? ReceivedAtUtc,
    Guid? ReceivedByUserId,
    DateTime? CancelledAtUtc,
    Guid? CancelledByUserId,
    IReadOnlyCollection<GetPackageItemResponse> Items,
    IReadOnlyCollection<GetPackageEventResponse> Events);

/// <summary>
/// Represents an asset included in a package.
/// </summary>
/// <param name="Id">
/// The unique identifier of the package item.
/// </param>
/// <param name="AssetId">
/// The identifier of the asset included in the package.
/// </param>
/// <param name="AddedAtUtc">
/// The UTC date and time when the asset was added to the package.
/// </param>
/// <param name="AddedByUserId">
/// The identifier of the user who added the asset to the package.
/// </param>
/// <param name="AssetName">
/// The name of the asset included in the package.
/// </param>
/// <param name="SerialNumber">
/// The serial number of the asset included in the package.
/// </param>
public sealed record GetPackageItemResponse(
    Guid Id,
    Guid AssetId,
    DateTime AddedAtUtc,
    Guid AddedByUserId,
    string AssetName,
    string SerialNumber);

/// <summary>
/// Represents an event recorded during the lifecycle of a package.
/// </summary>
/// <param name="Id">
/// The unique identifier of the package event.
/// </param>
/// <param name="Type">
/// The type of the package event.
/// </param>
/// <param name="PreviousStatus">
/// The package status before the event occurred.
/// </param>
/// <param name="NewStatus">
/// The package status after the event occurred.
/// </param>
/// <param name="RelatedAssetId">
/// The optional identifier of the asset associated with the event.
/// </param>
/// <param name="RelatedUserId">
/// The optional identifier of the user associated with the event,
/// such as the assigned courier.
/// </param>
/// <param name="Comment">
/// The optional comment associated with the event.
/// </param>
/// <param name="PerformedByUserId">
/// The identifier of the user who performed the operation.
/// </param>
/// <param name="PerformedByUserName">
/// The stored display name of the user who performed the operation.
/// </param>
/// <param name="OccurredAtUtc">
/// The UTC date and time when the event occurred.
/// </param>
public sealed record GetPackageEventResponse(
    Guid Id,
    PackageEventType Type,
    PackageStatus? PreviousStatus,
    PackageStatus NewStatus,
    Guid? RelatedAssetId,
    Guid? RelatedUserId,
    string? Comment,
    Guid PerformedByUserId,
    string PerformedByUserName,
    DateTime OccurredAtUtc);

public sealed record GetPackageListAssetResponse(
    Guid AssetId,
    string AssetName,
    string SerialNumber);