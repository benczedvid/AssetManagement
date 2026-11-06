using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.ReturnFromExternalService;

/// <summary>
/// Represents the result of successfully returning an asset from an external service provider.
/// </summary>
/// <param name="ServiceCaseId">The unique identifier of the service case.</param>
/// <param name="CaseNumber">The unique case number of the service case.</param>
/// <param name="AssetId">The unique identifier of the asset associated with the service case.</param>
/// <param name="ServiceCaseStatus">The current status of the service case.</param>
/// <param name="AssetStatus">The current status of the asset.</param>
/// <param name="VendorId">The unique identifier of the external service provider from which the asset was returned.</param>
/// <param name="ReturnedAtUtc">The date and time when the asset was returned from the external service provider, in UTC.</param>
/// <param name="ReturnedByUserId">The unique identifier of the user who recorded the return of the asset.</param>
/// <param name="ReturnedByUserName">The display name of the user who recorded the return of the asset.</param>
public sealed record ReturnFromExternalServiceResponse(
    Guid ServiceCaseId,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus ServiceCaseStatus,
    AssetStatus AssetStatus,
    Guid VendorId,
    DateTime ReturnedAtUtc,
    Guid ReturnedByUserId,
    string ReturnedByUserName);