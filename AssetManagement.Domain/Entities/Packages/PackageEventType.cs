namespace AssetManagement.Domain.Entities.Packages;

/// <summary>
/// Represents the type of an event recorded during the lifecycle of a package.
/// </summary>
public enum PackageEventType
{
    /// <summary>
    /// The package event type is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The package has been created.
    /// </summary>
    Created = 1,

    /// <summary>
    /// An asset has been added to the package.
    /// </summary>
    AssetAdded = 2,

    /// <summary>
    /// An asset has been removed from the package.
    /// </summary>
    AssetRemoved = 3,

    /// <summary>
    /// A courier has been assigned to the package.
    /// </summary>
    CourierAssigned = 4,

    /// <summary>
    /// The assigned courier has been changed.
    /// </summary>
    CourierChanged = 5,

    /// <summary>
    /// The assigned courier has been removed from the package.
    /// </summary>
    CourierRemoved = 6,

    /// <summary>
    /// The package has been dispatched from its source store.
    /// </summary>
    Dispatched = 7,

    /// <summary>
    /// The package has been received at its destination store.
    /// </summary>
    Received = 8,

    /// <summary>
    /// The package has been cancelled before dispatch.
    /// </summary>
    Cancelled = 9
}