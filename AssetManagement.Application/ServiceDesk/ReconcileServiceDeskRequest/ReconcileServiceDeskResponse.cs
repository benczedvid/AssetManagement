using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceDesk.ReconcileServiceDeskRequest;

public sealed record ReconcileServiceDeskResponse(
    Guid ServiceCaseId,
    Guid OperationId,
    ServiceDeskSyncStatus Status,
    string? ServiceDeskRequestId,
    DateTime? CompletedAtUtc);