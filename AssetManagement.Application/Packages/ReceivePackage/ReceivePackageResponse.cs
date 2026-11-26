using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.ReceivePackage;

/// <summary>
/// Represents the result of successfully receiving a package.
/// </summary>
/// <param name="PackageId">
/// The unique identifier of the package.
/// </param>
/// <param name="PackageNumber">
/// The business identifier of the package.
/// </param>
/// <param name="PackageStatus">
/// The current lifecycle status of the package.
/// </param>
/// <param name="DestinationStoreId">
/// The identifier of the destination store.
/// </param>
/// <param name="Assets">
/// The assets received in the package.
/// </param>
/// <param name="ReceivedAtUtc">
/// The UTC date and time when the package was received.
/// </param>
/// <param name="ReceivedByUserId">
/// The identifier of the user who received the package.
/// </param>
/// <param name="ReceivedByUserName">
/// The display name of the user who received the package.
/// </param>
public sealed record ReceivePackageResponse(
    Guid PackageId,
    string PackageNumber,
    PackageStatus PackageStatus,
    Guid DestinationStoreId,
    IReadOnlyCollection<ReceivePackageAssetResponse> Assets,
    DateTime ReceivedAtUtc,
    Guid ReceivedByUserId,
    string ReceivedByUserName);

/// <summary>
/// Represents an asset received in a package.
/// </summary>
/// <param name="AssetId">
/// The unique identifier of the asset.
/// </param>
/// <param name="AssignedStoreId">
/// The unchanged identifier of the store responsible for the asset.
/// </param>
/// <param name="PreviousStatus">
/// The asset status before receipt.
/// </param>
/// <param name="NewStatus">
/// The asset status after receipt.
/// </param>
public sealed record ReceivePackageAssetResponse(
    Guid AssetId,
    Guid AssignedStoreId,
    AssetStatus PreviousStatus,
    AssetStatus NewStatus);