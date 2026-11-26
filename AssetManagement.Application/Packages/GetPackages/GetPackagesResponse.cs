using AssetManagement.Application.Packages.GetPackageById;
using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.GetPackages;

/// <summary>
/// Represents the result of retrieving packages visible to the
/// current user.
/// </summary>
/// <param name="Items">
/// The packages visible within the current user's store access scope.
/// </param>
public sealed record GetPackagesResponse(IReadOnlyCollection<GetPackageListItemResponse> Items);

/// <summary>
/// Represents a package in the package list.
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
/// <param name="AssetCount">
/// The number of assets included in the package.
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
/// <param name="ReceivedAtUtc">
/// The optional UTC date and time when the package was received.
/// </param>
/// <param name="Assets">
/// The collection of assets included in the package.
/// </param>
public sealed record GetPackageListItemResponse(
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
    int AssetCount,
    DateTime CreatedAtUtc,
    Guid CreatedByUserId,
    DateTime? DispatchedAtUtc,
    DateTime? ReceivedAtUtc,
    IReadOnlyCollection<GetPackageListAssetResponse> Assets);

public sealed record GetPackageListAssetResponse(
    Guid AssetId,
    string AssetName,
    string SerialNumber);