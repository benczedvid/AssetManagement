namespace AssetManagement.Application.Packages.Exceptions;

/// <summary>
/// Represents an authorization failure for a package operation.
/// </summary>
public sealed class PackageAccessDeniedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PackageAccessDeniedException"/> class.
    /// </summary>
    /// <param name="message">
    /// The package-specific authorization error message.
    /// </param>
    public PackageAccessDeniedException(string message) : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }
}