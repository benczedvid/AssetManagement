namespace AssetManagement.Application.ServiceCases.DispatchToExternalService;

/// <summary>
/// Represents a request to dispatch an asset to the assigned external service provider.
/// </summary>
/// <param name="Comment">
/// An optional comment related to the dispatch of the asset to the external service provider.
/// </param>
public sealed record DispatchToExternalServiceRequest(
    string? Comment);