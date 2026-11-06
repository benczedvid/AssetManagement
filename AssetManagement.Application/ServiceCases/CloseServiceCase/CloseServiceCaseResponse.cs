using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.CloseServiceCase;

/// <summary>
/// Represents the result of a successfully closed service case.
/// </summary>
/// <param name="ServiceCaseId">The unique identifier of the service case.</param>
/// <param name="CaseNumber">The unique case number of the service case.</param>
/// <param name="AssetId">The unique identifier of the asset associated with the service case.</param>
/// <param name="ServiceCaseStatus">The current status of the service case.</param>
/// <param name="ClosedAtUtc">The date and time when the service case was closed, in UTC.</param>
/// <param name="ClosedByUserId">The unique identifier of the user who closed the service case.</param>
/// <param name="ClosedByUserName">The display name of the user who closed the service case.</param>
public sealed record CloseServiceCaseResponse(
    Guid ServiceCaseId,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus ServiceCaseStatus,
    DateTime ClosedAtUtc,
    Guid ClosedByUserId,
    string ClosedByUserName);