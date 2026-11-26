namespace AssetManagement.Application.Packages.CancelPackage;

/// <summary>
/// Represents a request to cancel a package before dispatch.
/// </summary>
/// <param name="Comment">
/// The required reason for cancelling the package.
/// </param>
public sealed record CancelPackageRequest(
    string Comment);