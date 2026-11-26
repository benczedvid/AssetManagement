namespace AssetManagement.Application.Packages.Exceptions;

/// <summary>
/// Represents a package operation that conflicts with the current
/// state of a package, asset, courier or related service case.
/// </summary>
public sealed class PackageStateConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PackageStateConflictException"/> class.
    /// </summary>
    /// <param name="message">
    /// The package-specific state conflict message.
    /// </param>
    public PackageStateConflictException(string message) : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }
}