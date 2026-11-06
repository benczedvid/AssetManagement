using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.ResolveAfterExternalServiceReturn;

/// <summary>
/// Represents the result of successfully resolving a service case after the asset has returned from external service.
/// </summary>
/// <param name="ServiceCaseId">The unique identifier of the service case.</param>
/// <param name="CaseNumber">The unique case number of the service case.</param>
/// <param name="AssetId">The unique identifier of the asset associated with the service case.</param>
/// <param name="ServiceCaseStatus">The current status of the service case.</param>
/// <param name="AssetStatus">The current status of the asset.</param>
/// <param name="VendorId">The unique identifier of the external service provider assigned to the service case.</param>
/// <param name="ResolvedAtUtc">The date and time when the service case was resolved, in UTC.</param>
/// <param name="ResolvedByUserId">The unique identifier of the user who resolved the service case.</param>
/// <param name="ResolvedByUserName">The display name of the user who resolved the service case.</param>
public sealed record ResolveAfterExternalServiceReturnResponse(
    Guid ServiceCaseId,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus ServiceCaseStatus,
    AssetStatus AssetStatus,
    Guid VendorId,
    DateTime ResolvedAtUtc,
    Guid ResolvedByUserId,
    string ResolvedByUserName);