namespace AssetManagement.Domain.Entities.Packages;

/// <summary>
/// Represents an asset included in a package.
/// </summary>
public sealed class PackageItem
{
    /// <summary>
    /// Gets the unique identifier of the package item.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the package containing the asset.
    /// </summary>
    public Guid PackageId { get; private set; }

    /// <summary>
    /// Gets the identifier of the asset included in the package.
    /// </summary>
    public Guid AssetId { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the asset was added to the package.
    /// </summary>
    public DateTime AddedAtUtc { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who added the asset to the package.
    /// </summary>
    public Guid AddedByUserId { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageItem"/> class.
    /// This constructor is required by Entity Framework Core.
    /// </summary>
    private PackageItem()
    {
    }

    private PackageItem(
        Guid id,
        Guid packageId,
        Guid assetId,
        DateTime addedAtUtc,
        Guid addedByUserId)
    {
        Id = id;
        PackageId = packageId;
        AssetId = assetId;
        AddedAtUtc = addedAtUtc;
        AddedByUserId = addedByUserId;
    }

    /// <summary>
    /// Creates a new package item.
    /// </summary>
    /// <param name="packageId">
    /// The identifier of the package containing the asset.
    /// </param>
    /// <param name="assetId">
    /// The identifier of the asset to add to the package.
    /// </param>
    /// <param name="addedAtUtc">
    /// The UTC date and time when the asset was added to the package.
    /// </param>
    /// <param name="addedByUserId">
    /// The identifier of the user who added the asset to the package.
    /// </param>
    /// <returns>
    /// A newly created package item.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an identifier is empty or the supplied date is invalid.
    /// </exception>
    public static PackageItem Create(
        Guid packageId,
        Guid assetId,
        DateTime addedAtUtc,
        Guid addedByUserId)
    {
        ValidateIdentifier(packageId, nameof(packageId), "Package identifier cannot be empty.");
        ValidateIdentifier(assetId, nameof(assetId), "Asset identifier cannot be empty.");

        ValidateAddedAtUtc(addedAtUtc);

        ValidateIdentifier(addedByUserId, nameof(addedByUserId), "Added by user identifier cannot be empty.");

        return new PackageItem(
            id: Guid.NewGuid(),
            packageId: packageId,
            assetId: assetId,
            addedAtUtc: addedAtUtc,
            addedByUserId: addedByUserId);
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName,
        string errorMessage)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(errorMessage,parameterName);
        }
    }

    private static void ValidateAddedAtUtc(DateTime addedAtUtc)
    {
        if (addedAtUtc == default)
        {
            throw new ArgumentException("Added date cannot be empty.", nameof(addedAtUtc));
        }

        if (addedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Added date must be in UTC.", nameof(addedAtUtc));
        }
    }
}