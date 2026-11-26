using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.RemovePackageAsset;

/// <summary>
/// Represents the result of removing an asset from a package.
/// </summary>
/// <param name="PackageId">
/// The unique identifier of the package.
/// </param>
/// <param name="PackageNumber">
/// The business identifier of the package.
/// </param>
/// <param name="Status">
/// The current lifecycle status of the package.
/// </param>
/// <param name="AssetId">
/// The identifier of the removed asset.
/// </param>
/// <param name="AssetCount">
/// The number of assets remaining in the package.
/// </param>
/// <param name="RemovedAtUtc">
/// The UTC timestamp when the asset was removed.
/// </param>
public sealed record RemovePackageAssetResponse(
    Guid PackageId,
    string PackageNumber,
    PackageStatus Status,
    Guid AssetId,
    int AssetCount,
    DateTime RemovedAtUtc);