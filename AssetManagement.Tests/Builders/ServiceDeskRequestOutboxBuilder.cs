using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Tests.Builders;

public sealed class ServiceDeskRequestOutboxBuilder
{
    private Guid _serviceCaseId = Guid.NewGuid();
    private Guid _serviceCaseEventId = Guid.NewGuid();
    private string? _subject = "SC-2026-0001 - Display issue";
    private string? _description = "The asset display does not work.";
    private DateTime _createdAtUtc = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    public ServiceDeskRequestOutboxBuilder WithServiceCaseId(Guid serviceCaseId)
    {
        _serviceCaseId = serviceCaseId;
        return this;
    }

    public ServiceDeskRequestOutboxBuilder WithServiceCaseEventId(Guid serviceCaseEventId)
    {
        _serviceCaseEventId = serviceCaseEventId;
        return this;
    }

    public ServiceDeskRequestOutboxBuilder WithSubject(string? subject)
    {
        _subject = subject;
        return this;
    }

    public ServiceDeskRequestOutboxBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public ServiceDeskRequestOutboxBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    public ServiceDeskRequestOutbox Build()
    {
        return ServiceDeskRequestOutbox.Create(
            serviceCaseId: _serviceCaseId,
            serviceCaseEventId: _serviceCaseEventId,
            subject: _subject!,
            description: _description!,
            createdAtUtc: _createdAtUtc);
    }
}