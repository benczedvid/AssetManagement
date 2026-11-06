namespace AssetManagement.Application.ServiceCases.ResolveAfterExternalServiceReturn;

/// <summary>
/// Represents a request to resolve a service case after the asset has returned from external service.
/// </summary>
/// <param name="Comment">
/// An optional comment related to the resolution of the service case.
/// If provided, the comment must not be empty or whitespace and must not
/// exceed 2000 characters.
/// </param>
public sealed record ResolveAfterExternalServiceReturnRequest(
    string? Comment);