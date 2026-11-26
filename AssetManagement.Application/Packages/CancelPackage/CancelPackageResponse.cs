using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.CancelPackage;

/// <summary>
/// Represents the result of successfully cancelling a package.
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
/// <param name="CancelledAtUtc">
/// The UTC date and time when the package was cancelled.
/// </param>
/// <param name="CancelledByUserId">
/// The identifier of the user who cancelled the package.
/// </param>
/// <param name="CancelledByUserName">
/// The display name of the user who cancelled the package.
/// </param>
/// <param name="Comment">
/// The reason provided for cancelling the package.
/// </param>
public sealed record CancelPackageResponse(
    Guid PackageId,
    string PackageNumber,
    PackageStatus PackageStatus,
    DateTime CancelledAtUtc,
    Guid CancelledByUserId,
    string CancelledByUserName,
    string Comment);