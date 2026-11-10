namespace AssetManagement.Domain.Entities.ServiceDesk;

public enum ServiceDeskReconciliationOperationType
{
    RequestCreation = 1,
    RequestClosure = 2
}

public enum ServiceDeskReconciliationAuditOutcome
{
    Completed = 1,
    Failed = 2
}

public sealed class ServiceDeskReconciliationAudit
{
    private ServiceDeskReconciliationAudit()
    {
    }

    public Guid Id { get; private set; }
    public ServiceDeskReconciliationOperationType Operation { get; private set; }
    public Guid OutboxId { get; private set; }
    public Guid ServiceCaseId { get; private set; }
    public Guid ProcessingOperationId { get; private set; }
    public ServiceDeskReconciliationAuditOutcome Outcome { get; private set; }
    public string? ExternalIdentifier { get; private set; }
    public Guid VerifiedByUserId { get; private set; }
    public string VerifiedByUserName { get; private set; } = string.Empty;
    public DateTime VerifiedAtUtc { get; private set; }
    public string Comment { get; private set; } = string.Empty;

    public static ServiceDeskReconciliationAudit Create(
        ServiceDeskReconciliationOperationType operation,
        Guid outboxId,
        Guid serviceCaseId,
        Guid processingOperationId,
        ServiceDeskReconciliationAuditOutcome outcome,
        string? externalIdentifier,
        Guid verifiedByUserId,
        string verifiedByUserName,
        DateTime verifiedAtUtc,
        string comment)
    {
        if (outboxId == Guid.Empty ||
            serviceCaseId == Guid.Empty ||
            processingOperationId == Guid.Empty ||
            verifiedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Audit identifiers cannot be empty.");
        }

        if (verifiedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The verification timestamp must be UTC.", nameof(verifiedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(verifiedByUserName))
        {
            throw new ArgumentException("The verifier name is required.", nameof(verifiedByUserName));
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("A verification comment is required.", nameof(comment));
        }

        return new ServiceDeskReconciliationAudit
        {
            Id = Guid.NewGuid(),
            Operation = operation,
            OutboxId = outboxId,
            ServiceCaseId = serviceCaseId,
            ProcessingOperationId = processingOperationId,
            Outcome = outcome,
            ExternalIdentifier = externalIdentifier?.Trim(),
            VerifiedByUserId = verifiedByUserId,
            VerifiedByUserName = verifiedByUserName.Trim(),
            VerifiedAtUtc = verifiedAtUtc,
            Comment = comment.Trim()
        };
    }
}