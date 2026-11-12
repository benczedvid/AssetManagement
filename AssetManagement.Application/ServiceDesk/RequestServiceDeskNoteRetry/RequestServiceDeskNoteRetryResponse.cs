namespace AssetManagement.Application.ServiceDesk.RequestServiceDeskNoteRetry;

public sealed record RequestServiceDeskNoteRetryResponse(
    Guid OutboxId,
    DateTime RequestedAtUtc);