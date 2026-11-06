namespace AssetManagement.Application.ServiceCases.DisposeServiceCaseAsset;

/// <summary>
/// Represents a request to dispose of the asset associated
/// with a service case.
/// </summary>
/// <param name="Reason">
/// The mandatory disposal reason.
/// The value cannot exceed 2000 characters after trimming.
/// </param>
/// <remarks>
/// The service case identifier is supplied through the endpoint route.
/// The associated asset is determined from the service case.
///
/// Only active application administrators may perform this operation.
///
/// An asset located at an external service provider may remain there
/// for donor parts. This circumstance should be described in the reason.
///
/// The reason is recorded in both the service case disposal event
/// and the asset disposal history.
/// </remarks>
public sealed record DisposeServiceCaseAssetRequest(
    string Reason);