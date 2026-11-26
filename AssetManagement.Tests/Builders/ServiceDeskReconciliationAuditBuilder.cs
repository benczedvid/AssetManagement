using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Tests.Builders;

public sealed class ServiceDeskReconciliationAuditBuilder
{
    private ServiceDeskReconciliationOperationType _operation = ServiceDeskReconciliationOperationType.RequestCreation;
    private Guid _outboxId = Guid.NewGuid();
    private Guid _serviceCaseId = Guid.NewGuid();
    private Guid _processingOperationId = Guid.NewGuid();
    private ServiceDeskReconciliationAuditOutcome _outcome = ServiceDeskReconciliationAuditOutcome.Completed;
    private string? _externalIdentifier;
    private Guid _verifiedByUserId = Guid.NewGuid();
    private string? _verifiedByUserName = "Test User";
    private DateTime _verifiedAtUtc = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);
    private string? _comment = "The external operation has been verified.";

    public ServiceDeskReconciliationAuditBuilder WithOperation(ServiceDeskReconciliationOperationType operation)
    {
        _operation = operation;
        return this;
    }

    public ServiceDeskReconciliationAuditBuilder WithOutboxId(Guid outboxId)
    {
        _outboxId = outboxId;
        return this;
    }

    public ServiceDeskReconciliationAuditBuilder WithServiceCaseId(Guid serviceCaseId)
    {
        _serviceCaseId = serviceCaseId;
        return this;
    }

    public ServiceDeskReconciliationAuditBuilder WithProcessingOperationId(Guid processingOperationId)
    {
        _processingOperationId = processingOperationId;
        return this;
    }

    public ServiceDeskReconciliationAuditBuilder WithOutcome(ServiceDeskReconciliationAuditOutcome outcome)
    {
        _outcome = outcome;
        return this;
    }

    public ServiceDeskReconciliationAuditBuilder WithExternalIdentifier(string? externalIdentifier)
    {
        _externalIdentifier = externalIdentifier;
        return this;
    }

    public ServiceDeskReconciliationAuditBuilder WithoutExternalIdentifier()
    {
        _externalIdentifier = null;
        return this;
    }

    public ServiceDeskReconciliationAuditBuilder WithVerifiedByUserId(Guid verifiedByUserId)
    {
        _verifiedByUserId = verifiedByUserId;
        return this;
    }

    public ServiceDeskReconciliationAuditBuilder WithVerifiedByUserName(string? verifiedByUserName)
    {
        _verifiedByUserName = verifiedByUserName;
        return this;
    }

    public ServiceDeskReconciliationAuditBuilder WithVerifiedAtUtc(DateTime verifiedAtUtc)
    {
        _verifiedAtUtc = verifiedAtUtc;
        return this;
    }

    public ServiceDeskReconciliationAuditBuilder WithComment(string? comment)
    {
        _comment = comment;
        return this;
    }

    public ServiceDeskReconciliationAudit Build()
    {
        return ServiceDeskReconciliationAudit.Create(
            operation: _operation,
            outboxId: _outboxId,
            serviceCaseId: _serviceCaseId,
            processingOperationId: _processingOperationId,
            outcome: _outcome,
            externalIdentifier: _externalIdentifier,
            verifiedByUserId: _verifiedByUserId,
            verifiedByUserName: _verifiedByUserName!,
            verifiedAtUtc: _verifiedAtUtc,
            comment: _comment!);
    }
}