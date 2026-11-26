using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Tests.Builders;

public sealed class ServiceCaseEventBuilder
{
    private Guid _serviceCaseId = Guid.NewGuid();
    private ServiceCaseEventType _type = ServiceCaseEventType.Created;
    private ServiceCaseStatus? _previousStatus;
    private ServiceCaseStatus _newStatus = ServiceCaseStatus.UnderCentralInspection;
    private string? _comment;
    private Guid _performedByUserId = Guid.NewGuid();
    private string? _performedByUserName = "Test User";
    private DateTime _occurredAtUtc = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    public ServiceCaseEventBuilder WithServiceCaseId(Guid serviceCaseId)
    {
        _serviceCaseId = serviceCaseId;
        return this;
    }

    public ServiceCaseEventBuilder WithType(ServiceCaseEventType type)
    {
        _type = type;
        return this;
    }

    public ServiceCaseEventBuilder WithPreviousStatus(ServiceCaseStatus? previousStatus)
    {
        _previousStatus = previousStatus;
        return this;
    }

    public ServiceCaseEventBuilder WithNewStatus(ServiceCaseStatus newStatus)
    {
        _newStatus = newStatus;
        return this;
    }

    public ServiceCaseEventBuilder WithComment(string? comment)
    {
        _comment = comment;
        return this;
    }

    public ServiceCaseEventBuilder WithPerformedByUserId(Guid performedByUserId)
    {
        _performedByUserId = performedByUserId;
        return this;
    }

    public ServiceCaseEventBuilder WithPerformedByUserName(string? performedByUserName)
    {
        _performedByUserName = performedByUserName;
        return this;
    }

    public ServiceCaseEventBuilder WithOccurredAtUtc(DateTime occurredAtUtc)
    {
        _occurredAtUtc = occurredAtUtc;
        return this;
    }

    public ServiceCaseEvent Build()
    {
        return ServiceCaseEvent.Create(
            serviceCaseId: _serviceCaseId,
            type: _type,
            previousStatus: _previousStatus,
            newStatus: _newStatus,
            comment: _comment,
            performedByUserId: _performedByUserId,
            performedByUserName: _performedByUserName!,
            occurredAtUtc: _occurredAtUtc);
    }
}