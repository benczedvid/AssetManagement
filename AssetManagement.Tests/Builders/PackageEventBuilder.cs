using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Tests.Builders;

public sealed class PackageEventBuilder
{
    private Guid _packageId = Guid.NewGuid();
    private PackageEventType _type = PackageEventType.Created;
    private PackageStatus? _previousStatus;
    private PackageStatus _newStatus = PackageStatus.Created;
    private Guid? _relatedAssetId;
    private Guid? _relatedUserId;
    private string? _comment;
    private Guid _performedByUserId = Guid.NewGuid();
    private string? _performedByUserName = "Test User";
    private DateTime _occurredAtUtc = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    public PackageEventBuilder WithPackageId(Guid packageId)
    {
        _packageId = packageId;
        return this;
    }

    public PackageEventBuilder WithType(PackageEventType type)
    {
        _type = type;
        return this;
    }

    public PackageEventBuilder WithPreviousStatus(PackageStatus? previousStatus)
    {
        _previousStatus = previousStatus;
        return this;
    }

    public PackageEventBuilder WithNewStatus(PackageStatus newStatus)
    {
        _newStatus = newStatus;
        return this;
    }

    public PackageEventBuilder WithRelatedAssetId(Guid? relatedAssetId)
    {
        _relatedAssetId = relatedAssetId;
        return this;
    }

    public PackageEventBuilder WithRelatedUserId(Guid? relatedUserId)
    {
        _relatedUserId = relatedUserId;
        return this;
    }

    public PackageEventBuilder WithComment(string? comment)
    {
        _comment = comment;
        return this;
    }

    public PackageEventBuilder WithPerformedByUserId(Guid performedByUserId)
    {
        _performedByUserId = performedByUserId;
        return this;
    }

    public PackageEventBuilder WithPerformedByUserName(string? performedByUserName)
    {
        _performedByUserName = performedByUserName;
        return this;
    }

    public PackageEventBuilder WithOccurredAtUtc(DateTime occurredAtUtc)
    {
        _occurredAtUtc = occurredAtUtc;
        return this;
    }

    public PackageEvent Build()
    {
        return PackageEvent.Create(
            packageId: _packageId,
            type: _type,
            previousStatus: _previousStatus,
            newStatus: _newStatus,
            relatedAssetId: _relatedAssetId,
            relatedUserId: _relatedUserId,
            comment: _comment,
            performedByUserId: _performedByUserId,
            performedByUserName: _performedByUserName!,
            occurredAtUtc: _occurredAtUtc);
    }
}