using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.HandOverToExternalService;

/// <summary>
/// Represents the result of successfully handing over an asset directly to an external service provider.
/// </summary>
/// <param name="ServiceCaseId">The unique identifier of the service case.</param>
/// <param name="CaseNumber">The unique case number of the service case.</param>
/// <param name="AssetId">The unique identifier of the asset associated with the service case.</param>
/// <param name="ServiceCaseStatus">The current status of the service case.</param>
/// <param name="AssetStatus">The current status of the asset.</param>
/// <param name="VendorId">The unique identifier of the external service provider to which the asset was handed over.</param>
/// <param name="HandedOverAtUtc">The date and time when the asset was handed over to the external service provider, in UTC.</param>
/// <param name="HandedOverByUserId">The unique identifier of the user who handed over the asset.</param>
/// <param name="HandedOverByUserName">The display name of the user who handed over the asset.</param>
public sealed record HandOverToExternalServiceResponse(
    Guid ServiceCaseId,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus ServiceCaseStatus,
    AssetStatus AssetStatus,
    Guid VendorId,
    DateTime HandedOverAtUtc,
    Guid HandedOverByUserId,
    string HandedOverByUserName);