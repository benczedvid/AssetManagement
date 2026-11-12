namespace AssetManagement.Application.ServiceDesk.Reconciliation;

public enum ServiceDeskReconciliationOperation
{
    RequestCreation = 1,
    RequestClosure = 2,
    NoteCreation = 3
}

public enum ServiceDeskReconciliationOutcome
{
    Completed = 1,
    Failed = 2
}

public sealed record ResolveServiceDeskReconciliationRequest(
    ServiceDeskReconciliationOperation Operation,
    Guid OutboxId,
    Guid ProcessingOperationId,
    ServiceDeskReconciliationOutcome Outcome,
    string? ServiceDeskRequestId,
    string VerificationComment,
    string? ServiceDeskNoteId = null);

public sealed record ResolveServiceDeskReconciliationResponse(
    ServiceDeskReconciliationOperation Operation,
    Guid OutboxId,
    ServiceDeskReconciliationOutcome Outcome,
    DateTime VerifiedAtUtc);