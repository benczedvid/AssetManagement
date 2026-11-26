namespace AssetManagement.Domain.Entities.Packages;

/// <summary>
/// Represents the current lifecycle status of a package.
/// </summary>
public enum PackageStatus
{
    /// <summary>
    /// The package status is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The package has been created but has not yet been dispatched.
    /// </summary>
    Created = 1,

    /// <summary>
    /// The package has been dispatched and is currently in transit.
    /// </summary>
    InTransit = 2,

    /// <summary>
    /// The package has been received at its destination store.
    /// </summary>
    Received = 3,

    /// <summary>
    /// The package was cancelled before dispatch.
    /// </summary>
    Cancelled = 4
}