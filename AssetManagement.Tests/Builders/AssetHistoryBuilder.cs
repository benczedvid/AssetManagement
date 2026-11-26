using AssetManagement.Domain.Entities.AssetHistories;

namespace AssetManagement.Tests.Builders;

public sealed class AssetHistoryBuilder
{
    private Guid _assetId = Guid.NewGuid();
    private string? _action = "Updated";
    private string? _propertyName = "SerialNumber";
    private string? _oldValue = "OLD-001";
    private string? _newValue = "NEW-001";
    private Guid _changedByUserId = Guid.NewGuid();
    private string? _changedByUserName = "Test User";
    private DateTime _changedAtUtc = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    public AssetHistoryBuilder WithAssetId(Guid assetId)
    {
        _assetId = assetId;
        return this;
    }

    public AssetHistoryBuilder WithAction(string? action)
    {
        _action = action;
        return this;
    }

    public AssetHistoryBuilder WithPropertyName(string? propertyName)
    {
        _propertyName = propertyName;
        return this;
    }

    public AssetHistoryBuilder WithOldValue(string? oldValue)
    {
        _oldValue = oldValue;
        return this;
    }

    public AssetHistoryBuilder WithNewValue(string? newValue)
    {
        _newValue = newValue;
        return this;
    }

    public AssetHistoryBuilder WithChangedByUserId(Guid changedByUserId)
    {
        _changedByUserId = changedByUserId;
        return this;
    }

    public AssetHistoryBuilder WithChangedByUserName(string? changedByUserName)
    {
        _changedByUserName = changedByUserName;
        return this;
    }

    public AssetHistoryBuilder WithChangedAtUtc(DateTime changedAtUtc)
    {
        _changedAtUtc = changedAtUtc;
        return this;
    }

    public AssetHistory Build()
    {
        return new AssetHistory(
            assetId: _assetId,
            action: _action!,
            propertyName: _propertyName!,
            oldValue: _oldValue,
            newValue: _newValue,
            changedByUserId: _changedByUserId,
            changedByUserName: _changedByUserName!,
            changedAtUtc: _changedAtUtc);
    }
}