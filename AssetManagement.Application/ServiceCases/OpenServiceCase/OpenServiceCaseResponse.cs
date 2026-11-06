using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.OpenServiceCase;

/// <summary>
/// Represents the result of successfully opening a new service case.
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
/// <param name="AssetStatus">The current status of the asset associated with the service case.</param>
/// <param name="CreatedByUserId">The unique identifier of the user who created the service case.</param>
/// <param name="CreatedByUserName">The display name of the user who created the service case.</param>
/// <param name="CreatedAtUtc">The date and time when the service case was created, in UTC.</param>
public sealed record OpenServiceCaseResponse(
    Guid Id,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus Status,
    ServiceIssueType IssueType,
    string? OtherIssueDescription,
    string Description,
    Guid? OriginStoreId,
    Guid? VendorId,
    AssetStatus AssetStatus,
    Guid CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAtUtc
    );