using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.OpenServiceCase;

/// <summary>
/// Represents a request to open a new service case for an asset.
/// </summary>
/// <param name="AssetId">
/// The unique identifier of the asset for which the service case is opened.
/// </param>
/// <param name="IssueType">
/// The type of issue reported for the asset.
/// </param>
/// <param name="OtherIssueDescription">
/// An additional description of the issue when the selected issue type requires further information.
/// </param>
/// <param name="Description">
/// The detailed description of the reported issue.
/// </param>
public sealed record OpenServiceCaseRequest(
    Guid AssetId,
    ServiceIssueType IssueType,
    string? OtherIssueDescription,
    string Description
    );