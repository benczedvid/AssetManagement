using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.AssignCourier;

/// <summary>
/// Represents the result of assigning, changing or removing
/// the courier of a package.
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
/// <param name="CourierUserId">
/// The optional identifier of the currently assigned courier.
/// </param>
/// <param name="UpdatedAtUtc">
/// The UTC date and time when the courier assignment was updated.
/// </param>
public sealed record AssignCourierResponse(
    Guid PackageId,
    string PackageNumber,
    PackageStatus PackageStatus,
    Guid? CourierUserId,
    DateTime UpdatedAtUtc);