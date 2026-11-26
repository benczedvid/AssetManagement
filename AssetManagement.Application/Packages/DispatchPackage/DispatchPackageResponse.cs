using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.DispatchPackage;

/// <summary>
/// Represents the result of successfully dispatching a package.
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
/// <param name="SourceStoreId">
/// The identifier of the source store.
/// </param>
/// <param name="DestinationStoreId">
/// The identifier of the destination store.
/// </param>
/// <param name="CourierUserId">
/// The optional identifier of the assigned courier.
/// </param>
/// <param name="Assets">
/// The assets dispatched in the package.
/// </param>
/// <param name="DispatchedAtUtc">
/// The UTC date and time when the package was dispatched.
/// </param>
/// <param name="DispatchedByUserId">
/// The identifier of the user who dispatched the package.
/// </param>
/// <param name="DispatchedByUserName">
/// The display name of the user who dispatched the package.
/// </param>
public sealed record DispatchPackageResponse(
    Guid PackageId,
    string PackageNumber,
    PackageStatus PackageStatus,
    Guid SourceStoreId,
    Guid DestinationStoreId,
    Guid? CourierUserId,
    IReadOnlyCollection<DispatchPackageAssetResponse> Assets,
    DateTime DispatchedAtUtc,
    Guid DispatchedByUserId,
    string DispatchedByUserName);

/// <summary>
/// Represents an asset dispatched in a package.
/// </summary>
/// <param name="AssetId">
/// The unique identifier of the asset.
/// </param>
/// <param name="PreviousStatus">
/// The asset status before package dispatch.
/// </param>
/// <param name="NewStatus">
/// The asset status after package dispatch.
/// </param>
public sealed record DispatchPackageAssetResponse(
    Guid AssetId,
    AssetStatus PreviousStatus,
    AssetStatus NewStatus);