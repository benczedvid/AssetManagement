namespace AssetManagement.Application.Packages.DispatchPackage;

/// <summary>
/// Represents a request to dispatch a package from its source store.
/// </summary>
/// <param name="Comment">
/// An optional comment describing the package dispatch.
/// </param>
public sealed record DispatchPackageRequest(
    string? Comment);