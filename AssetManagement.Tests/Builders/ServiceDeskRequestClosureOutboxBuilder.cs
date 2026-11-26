using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Tests.Builders;

public sealed class ServiceDeskRequestClosureOutboxBuilder
{
    private Guid _serviceCaseId = Guid.NewGuid();
    private Guid _serviceCaseEventId = Guid.NewGuid();
    private DateTime _createdAtUtc = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    public ServiceDeskRequestClosureOutboxBuilder WithServiceCaseId(Guid serviceCaseId)
    {
        _serviceCaseId = serviceCaseId;
        return this;
    }

    public ServiceDeskRequestClosureOutboxBuilder WithServiceCaseEventId(Guid serviceCaseEventId)
    {
        _serviceCaseEventId = serviceCaseEventId;
        return this;
    }

    public ServiceDeskRequestClosureOutboxBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    public ServiceDeskRequestClosureOutbox Build()
    {
        return ServiceDeskRequestClosureOutbox.Create(
            serviceCaseId: _serviceCaseId,
            serviceCaseEventId: _serviceCaseEventId,
            createdAtUtc: _createdAtUtc);
    }
}