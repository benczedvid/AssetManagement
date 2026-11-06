using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.GetServiceCaseDetails;

/// <summary>
/// Represents an event recorded in the history of a service case.
/// </summary>
/// <param name="Id">The unique identifier of the service case event.</param>
/// <param name="Type">The type of event that occurred.</param>
/// <param name="PreviousStatus">The status of the service case before the event occurred, when applicable.</param>
/// <param name="NewStatus">The status of the service case after the event occurred.</param>
/// <param name="Comment">An optional comment associated with the event.</param>
/// <param name="PerformedByUserId">The unique identifier of the user who performed the action.</param>
/// <param name="PerformedByUserName">The display name of the user who performed the action.</param>
/// <param name="OccurredAtUtc">The date and time when the event occurred, in UTC.</param>
public sealed record ServiceCaseEventResponse(
    Guid Id,
    ServiceCaseEventType Type,
    ServiceCaseStatus? PreviousStatus,
    ServiceCaseStatus NewStatus,
    string? Comment,
    Guid PerformedByUserId,
    string PerformedByUserName,
    DateTime OccurredAtUtc);