using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.RequireExternalService;

/// <summary>
/// Represents the result of successfully marking a service case as requiring external service.
/// </summary>
/// <param name="ServiceCaseId">The unique identifier of the service case.</param>
/// <param name="CaseNumber">The unique case number of the service case.</param>
/// <param name="AssetId">The unique identifier of the asset associated with the service case.</param>
/// <param name="ServiceCaseStatus">The current status of the service case.</param>
/// <param name="AssetStatus">The current status of the asset.</param>
/// <param name="VendorId">The unique identifier of the external service provider assigned to the service case.</param>
/// <param name="DecidedAtUtc">The date and time when the decision to require external service was made, in UTC.</param>
/// <param name="DecidedByUserId">The unique identifier of the user who made the external service decision.</param>
/// <param name="DecidedByUserName">The display name of the user who made the external service decision.</param>
public sealed record RequireExternalServiceResponse(
    Guid ServiceCaseId,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus ServiceCaseStatus,
    AssetStatus AssetStatus,
    Guid VendorId,
    DateTime DecidedAtUtc,
    Guid DecidedByUserId,
    string DecidedByUserName
    );