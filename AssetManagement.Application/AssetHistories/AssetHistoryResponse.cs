namespace AssetManagement.Application.AssetHistories
{
    /// <summary>
    /// Represents a change recorded in the history of an asset.
    /// </summary>
    /// <param name="Id">The unique identifier of the history entry.</param>
    /// <param name="AssetId">The unique identifier of the asset associated with the change.</param>
    /// <param name="Action">The action that caused the change.</param>
    /// <param name="PropertyName">The name of the asset property that was changed.</param>
    /// <param name="OldValue">The value of the property before the change, when available.</param>
    /// <param name="NewValue">The value of the property after the change, when available.</param>
    /// <param name="ChangedByUserId">The unique identifier of the user who performed the change.</param>
    /// <param name="ChangedByUserName">The display name of the user who performed the change.</param>
    /// <param name="ChangedAtUtc">The date and time when the change occurred, in UTC.</param>
    public sealed record AssetHistoryResponse
        (
        Guid Id,
        Guid AssetId,
        string Action,
        string PropertyName,
        string? OldValue,
        string? NewValue,
        Guid ChangedByUserId,
        string ChangedByUserName,
        DateTime ChangedAtUtc
        );
}
