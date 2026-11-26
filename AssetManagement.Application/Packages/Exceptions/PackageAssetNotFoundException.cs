namespace AssetManagement.Application.Packages.Exceptions;

/// <summary>
/// Represents an error that occurs when an asset required by
/// a package operation cannot be found.
/// </summary>
public sealed class PackageAssetNotFoundException : Exception
{
    /// <summary>
    /// Gets the identifier of the asset that could not be found.
    /// </summary>
    public Guid AssetId { get; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PackageAssetNotFoundException"/> class.
    /// </summary>
    /// <param name="assetId">
    /// The identifier of the asset that could not be found.
    /// </param>
    public PackageAssetNotFoundException(Guid assetId) : base($"The asset with identifier '{assetId}' required by the package operation could not be found.")
    {
        AssetId = assetId;
    }
}