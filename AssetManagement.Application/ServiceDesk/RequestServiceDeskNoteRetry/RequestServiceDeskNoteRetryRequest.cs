namespace AssetManagement.Application.ServiceDesk.RequestServiceDeskNoteRetry;

public sealed record RequestServiceDeskNoteRetryRequest(
    Guid OutboxId,
    Guid ProcessingOperationId,
    string Comment);