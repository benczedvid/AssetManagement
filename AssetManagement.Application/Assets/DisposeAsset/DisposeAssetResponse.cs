using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.DisposeAsset{
    /// <summary>
    /// Represents the result of successfully disposing of an asset.
    /// </summary>
    /// <param name="AssetId">
    /// The unique identifier of the disposed asset.
    /// </param>
    /// <param name="AssetName">
    /// The name of the disposed asset.
    /// </param>
    /// <param name="AssetStatus">
    /// The asset status after disposal.
    /// </param>
    /// <param name="Reason">
    /// The recorded reason for disposing of the asset.
    /// </param>
    /// <param name="DisposedAtUtc">
    /// The UTC timestamp when the asset was disposed.
    /// </param>
    /// <param name="DisposedByUserId">
    /// The internal application user identifier of the administrator
    /// who performed the disposal.
    /// </param>
    /// <param name="DisposedByUserName">
    /// The name of the administrator who performed the disposal.
    /// </param>
    public sealed record DisposeAssetResponse(
        Guid AssetId,
        string AssetName,
        AssetStatus AssetStatus,
        string Reason,
        DateTime DisposedAtUtc,
        Guid DisposedByUserId,
        string DisposedByUserName);
}
