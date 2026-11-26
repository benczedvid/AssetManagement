using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.CreatePackage;

/// <summary>
/// Represents the result of successfully creating a package.
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
/// The identifier of the store from which the package will be dispatched.
/// </param>
/// <param name="DestinationStoreId">
/// The identifier of the store at which the package will be received.
/// </param>
/// <param name="CourierUserId">
/// The optional identifier of the application user assigned
/// to deliver the package.
/// </param>
/// <param name="AssetIds">
/// The identifiers of the assets included in the package.
/// </param>
/// <param name="CreatedAtUtc">
/// The UTC date and time when the package was created.
/// </param>
public sealed record CreatePackageResponse(
    Guid Id,
    string PackageNumber,
    PackageStatus Status,
    Guid SourceStoreId,
    Guid DestinationStoreId,
    Guid? CourierUserId,
    IReadOnlyCollection<Guid> AssetIds,
    DateTime CreatedAtUtc);