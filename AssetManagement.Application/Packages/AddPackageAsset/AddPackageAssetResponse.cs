using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.AddPackageAsset;

/// <summary>
/// Represents the result of adding an asset to a package.
/// </summary>
/// <param name="PackageId">
/// The unique identifier of the package.
/// </param>
/// <param name="PackageNumber">
/// The business identifier of the package.
/// </param>
/// <param name="Status">
/// The current package status.
/// </param>
/// <param name="AssetId">
/// The identifier of the added asset.
/// </param>
/// <param name="AssetCount">
/// The number of assets currently included in the package.
/// </param>
/// <param name="AddedAtUtc">
/// The UTC timestamp when the asset was added.
/// </param>
public sealed record AddPackageAssetResponse(
    Guid PackageId,
    string PackageNumber,
    PackageStatus Status,
    Guid AssetId,
    int AssetCount,
    DateTime AddedAtUtc);