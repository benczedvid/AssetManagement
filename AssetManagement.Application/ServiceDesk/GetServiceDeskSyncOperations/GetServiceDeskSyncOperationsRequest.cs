namespace AssetManagement.Application.ServiceDesk.GetServiceDeskSyncOperations;

public sealed record GetServiceDeskSyncOperationsRequest(
    string? Status = null,
    string? OperationType = null,
    Guid? ServiceCaseId = null);