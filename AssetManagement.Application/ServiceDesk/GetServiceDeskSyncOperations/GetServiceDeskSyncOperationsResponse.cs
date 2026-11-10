namespace AssetManagement.Application.ServiceDesk.GetServiceDeskSyncOperations;

public sealed record ServiceDeskSyncOperationItem(
    Guid OutboxId,
    Guid ServiceCaseId,
    Guid ServiceCaseEventId,
    string OperationType,
    string Status,
    Guid? ProcessingOperationId,
    string? ExternalIdentifier,
    DateTime CreatedAtUtc,
    DateTime? ProcessingStartedAtUtc,
    DateTime? ProcessedAtUtc,
    string? Error,
    string ServiceCaseNumber);

public sealed record GetServiceDeskSyncOperationsResponse(
    IReadOnlyList<ServiceDeskSyncOperationItem> Items);