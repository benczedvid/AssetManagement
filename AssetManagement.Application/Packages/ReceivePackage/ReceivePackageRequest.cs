namespace AssetManagement.Application.Packages.ReceivePackage;

/// <summary>
/// Represents a request to receive a package at its destination store.
/// </summary>
/// <param name="ReceivingStoreId">
/// The identifier of the store receiving the package.
/// This value must match the package destination store.
/// </param>
/// <param name="Comment">
/// An optional comment describing the package receipt.
/// </param>
public sealed record ReceivePackageRequest(
    Guid ReceivingStoreId,
    string? Comment);