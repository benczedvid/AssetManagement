namespace AssetManagement.Application.Packages.CreatePackage;

/// <summary>
/// Represents a request to create a package for transporting assets
/// between stores or between a store and headquarters.
/// </summary>
/// <param name="SourceStoreId">
/// The identifier of the store from which the package will be dispatched.
/// </param>
/// <param name="DestinationStoreId">
/// The identifier of the store at which the package will be received.
/// </param>
/// <param name="AssetIds">
/// The identifiers of the assets to include in the package.
/// At least one asset must be specified.
/// </param>
/// <param name="CourierUserId">
/// The optional identifier of the application user assigned
/// to deliver the package.
/// </param>
/// <param name="Comment">
/// An optional comment describing the package or delivery.
/// </param>
public sealed record CreatePackageRequest(
    Guid SourceStoreId,
    Guid DestinationStoreId,
    IReadOnlyCollection<Guid> AssetIds,
    Guid? CourierUserId,
    string? Comment);