namespace AssetManagement.Application.ServiceCases.ConfirmReceivedByExternalService;

/// <summary>
/// Represents a request to confirm that an asset has been received by the external service provider.
/// </summary>
/// <param name="Comment">
/// An optional comment related to the receipt confirmation.
/// </param>
public sealed record ConfirmReceivedByExternalServiceRequest(
    string? Comment);