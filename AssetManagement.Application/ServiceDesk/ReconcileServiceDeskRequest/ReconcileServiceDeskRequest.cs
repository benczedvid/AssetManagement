namespace AssetManagement.Application.ServiceDesk.ReconcileServiceDeskRequest;

public sealed record ReconcileServiceDeskRequest(
    Guid ServiceCaseId,
    Guid OperationId,
    bool RequestExists,
    string? ServiceDeskRequestId,
    string? Comment);