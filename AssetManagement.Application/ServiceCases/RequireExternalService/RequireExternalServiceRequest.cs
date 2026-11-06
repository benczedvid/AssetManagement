namespace AssetManagement.Application.ServiceCases.RequireExternalService;

/// <summary>
/// Represents a request to mark a service case as requiring external service.
/// </summary>
/// <param name="VendorId">
/// The unique identifier of the external service provider assigned to the service case.
/// </param>
/// <param name="Comment">
/// An optional comment related to the decision to require external service.
/// </param>
public sealed record RequireExternalServiceRequest(
    Guid VendorId,
    string? Comment
    );