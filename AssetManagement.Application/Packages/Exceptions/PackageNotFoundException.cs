namespace AssetManagement.Application.Packages.Exceptions;

/// <summary>
/// Represents an error that occurs when a package cannot be found.
/// </summary>
public sealed class PackageNotFoundException : Exception
{
    /// <summary>
    /// Gets the identifier of the package that could not be found.
    /// </summary>
    public Guid PackageId { get; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PackageNotFoundException"/> class.
    /// </summary>
    /// <param name="packageId">
    /// The identifier of the package that could not be found.
    /// </param>
    public PackageNotFoundException(Guid packageId) : base($"The package with identifier '{packageId}' could not be found.")
    {
        PackageId = packageId;
    }
}