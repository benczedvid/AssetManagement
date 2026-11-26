namespace AssetManagement.Application.Packages.AddPackageAsset;

/// <summary>
/// Represents a request to add an asset to an existing package.
/// </summary>
/// <param name="AssetId">
/// The unique identifier of the asset to add.
/// </param>
public sealed record AddPackageAssetRequest(
    Guid AssetId);