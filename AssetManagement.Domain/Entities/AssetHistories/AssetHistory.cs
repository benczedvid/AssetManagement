namespace AssetManagement.Domain.Entities.AssetHistories;

public sealed class AssetHistory
{
    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string PropertyName { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public string ChangedByUserName { get; private set; } = string.Empty;
    public DateTime ChangedAtUtc { get; private set; }

    private AssetHistory()
    {
    }

    public AssetHistory(
        Guid assetId,
        string action,
        string propertyName,
        string? oldValue,
        string? newValue,
        Guid changedByUserId,
        string changedByUserName,
        DateTime changedAtUtc)
    {
        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("Asset ID cannot be empty.", nameof(assetId));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action cannot be empty.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be empty.", nameof(propertyName));
        }

        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            throw new ArgumentException("Old value and new value cannot be the same.", nameof(newValue));
        }

        if (changedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Changed by user ID cannot be empty.", nameof(changedByUserId));
        }

        if (string.IsNullOrWhiteSpace(changedByUserName))
        {
            throw new ArgumentException("Changed by user name cannot be empty.", nameof(changedByUserName));
        }

        if (changedAtUtc == default)
        {
            throw new ArgumentException("Changed date cannot be empty.", nameof(changedAtUtc));
        }

        if (changedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Changed date must be in UTC.", nameof(changedAtUtc));
        }

        Id = Guid.NewGuid();
        AssetId = assetId;
        Action = action.Trim();
        PropertyName = propertyName.Trim();
        OldValue = oldValue;
        NewValue = newValue;
        ChangedByUserId = changedByUserId;
        ChangedByUserName = changedByUserName.Trim();
        ChangedAtUtc = changedAtUtc;
    }


}