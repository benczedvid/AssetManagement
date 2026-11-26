namespace AssetManagement.Application.Packages.AssignCourier;

/// <summary>
/// Represents a request to assign, change or remove the courier
/// of a package.
/// </summary>
/// <param name="CourierUserId">
/// The optional identifier of the application user assigned
/// to deliver the package.
///
/// When the value is null, the currently assigned courier
/// is removed from the package.
/// </param>
/// <param name="Comment">
/// An optional comment describing the courier assignment change.
/// </param>
public sealed record AssignCourierRequest(
    Guid? CourierUserId,
    string? Comment);