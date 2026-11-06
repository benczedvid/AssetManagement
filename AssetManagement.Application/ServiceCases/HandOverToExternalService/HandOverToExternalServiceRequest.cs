namespace AssetManagement.Application.ServiceCases.HandOverToExternalService;

/// <summary>
/// Represents a request to hand over an asset directly to the assigned external service provider.
/// </summary>
/// <param name="Comment">
/// An optional comment related to the direct hand-over of the asset to the external service provider.
/// </param>
public sealed record HandOverToExternalServiceRequest(
    string? Comment);