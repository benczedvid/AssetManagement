using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.CompleteRepairAtHeadquarters;

/// <summary>
/// Represents the result of a successfully completed repair at headquarters.
/// </summary>
/// <param name="ServiceCaseId">The unique identifier of the service case.</param>
/// <param name="CaseNumber">The unique case number of the service case.</param>
/// <param name="AssetId">The unique identifier of the asset associated with the service case.</param>
/// <param name="ServiceCaseStatus">The current status of the service case.</param>
/// <param name="AssetStatus">The current status of the asset.</param>
/// <param name="CompletedAtUtc">The date and time when the repair was completed at headquarters, in UTC.</param>
/// <param name="CompletedByUserId">The unique identifier of the user who completed the repair.</param>
/// <param name="CompletedByUserName">The display name of the user who completed the repair.</param>
public sealed record CompleteRepairAtHeadquartersResponse(
    Guid ServiceCaseId,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus ServiceCaseStatus,
    AssetStatus AssetStatus,
    DateTime CompletedAtUtc,
    Guid CompletedByUserId,
    string CompletedByUserName);