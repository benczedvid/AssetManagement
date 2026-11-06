namespace AssetManagement.Application.ServiceCases.ReturnFromExternalService;

/// <summary>
/// Represents a request to record the return of an asset from an external service provider.
/// </summary>
/// <param name="Comment">
/// An optional comment related to the return of the asset from the external service provider.
/// </param>
public sealed record ReturnFromExternalServiceRequest(
    string? Comment);