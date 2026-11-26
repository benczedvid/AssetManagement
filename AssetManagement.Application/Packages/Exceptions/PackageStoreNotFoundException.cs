namespace AssetManagement.Application.Packages.Exceptions;

/// <summary>
/// Represents an error that occurs when a store required by
/// a package operation cannot be found.
/// </summary>
public sealed class PackageStoreNotFoundException : Exception
{
    /// <summary>
    /// Gets the identifier of the store that could not be found.
    /// </summary>
    public Guid StoreId { get; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PackageStoreNotFoundException"/> class.
    /// </summary>
    /// <param name="storeId">
    /// The identifier of the store that could not be found.
    /// </param>
    public PackageStoreNotFoundException(Guid storeId) : base($"The store with identifier '{storeId}' required by the package operation could not be found.")
    {
        StoreId = storeId;
    }
}