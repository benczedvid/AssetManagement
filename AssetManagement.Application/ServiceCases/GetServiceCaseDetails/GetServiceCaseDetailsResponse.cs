using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.GetServiceCaseDetails;

/// <summary>
/// Represents detailed information about a service case returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the service case.</param>
/// <param name="CaseNumber">The unique case number of the service case.</param>
/// <param name="AssetId">The unique identifier of the asset associated with the service case.</param>
/// <param name="Status">The current status of the service case.</param>
/// <param name="IssueType">The type of issue reported for the service case.</param>
/// <param name="OtherIssueDescription">
/// The description of the issue when the selected issue type requires additional information.
/// </param>
/// <param name="Description">The detailed description of the reported issue.</param>
/// <param name="OriginStoreId">
/// The unique identifier of the store from which the service case originated, when applicable.
/// </param>
/// <param name="VendorId">
/// The unique identifier of the external service provider assigned to the service case, when applicable.
/// </param>
/// <param name="IsActive">Indicates whether the service case is currently active.</param>
/// <param name="CreatedByUserId">The unique identifier of the user who created the service case.</param>
/// <param name="CreatedByUserName">The display name of the user who created the service case.</param>
/// <param name="CreatedAtUtc">The date and time when the service case was created, in UTC.</param>
/// <param name="ClosedByUserId">
/// The unique identifier of the user who closed the service case, when applicable.
/// </param>
/// <param name="ClosedAtUtc">
/// The date and time when the service case was closed, in UTC, when applicable.
/// </param>
/// <param name="Events">
/// The recorded event history of the service case.
/// </param>
/// <param name="AvailableActions">
/// The actions currently available to the authenticated user based on the service case state and the user's permissions.
/// </param>
/// /// <param name="ServiceDeskRequestId">
/// The external ServiceDesk Plus request identifier,
/// when synchronization has completed.
/// </param>
public sealed record GetServiceCaseDetailsResponse(
    Guid Id,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus Status,
    ServiceIssueType IssueType,
    string? OtherIssueDescription,
    string Description,
    Guid? OriginStoreId,
    Guid? VendorId,
    bool IsActive,
    Guid CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAtUtc,
    Guid? ClosedByUserId,
    DateTime? ClosedAtUtc,
    IReadOnlyList<ServiceCaseEventResponse> Events,
    IReadOnlyList<string> AvailableActions,
    string? ServiceDeskRequestId);