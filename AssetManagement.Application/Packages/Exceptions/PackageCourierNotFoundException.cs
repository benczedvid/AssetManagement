namespace AssetManagement.Application.Packages.Exceptions;

/// <summary>
/// Represents an error that occurs when a selected package courier
/// cannot be found.
/// </summary>
public sealed class PackageCourierNotFoundException : Exception
{
    /// <summary>
    /// Gets the identifier of the courier that could not be found.
    /// </summary>
    public Guid CourierUserId { get; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PackageCourierNotFoundException"/> class.
    /// </summary>
    /// <param name="courierUserId">
    /// The identifier of the courier that could not be found.
    /// </param>
    public PackageCourierNotFoundException(Guid courierUserId) : base($"The courier user with identifier '{courierUserId}' could not be found.")
    {
        CourierUserId = courierUserId;
    }
}