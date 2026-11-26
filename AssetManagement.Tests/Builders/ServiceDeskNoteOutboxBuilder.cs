using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Tests.Builders;

public sealed class ServiceDeskNoteOutboxBuilder
{
    private Guid _serviceCaseId = Guid.NewGuid();
    private Guid _serviceCaseEventId = Guid.NewGuid();
    private string? _description = "The asset has been received at headquarters.";
    private DateTime _createdAtUtc = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    public ServiceDeskNoteOutboxBuilder WithServiceCaseId(Guid serviceCaseId)
    {
        _serviceCaseId = serviceCaseId;
        return this;
    }

    public ServiceDeskNoteOutboxBuilder WithServiceCaseEventId(Guid serviceCaseEventId)
    {
        _serviceCaseEventId = serviceCaseEventId;
        return this;
    }

    public ServiceDeskNoteOutboxBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public ServiceDeskNoteOutboxBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    public ServiceDeskNoteOutbox Build()
    {
        return ServiceDeskNoteOutbox.Create(
            serviceCaseId: _serviceCaseId,
            serviceCaseEventId: _serviceCaseEventId,
            description: _description!,
            createdAtUtc: _createdAtUtc);
    }
}