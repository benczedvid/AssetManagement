namespace AssetManagement.Application.Assets.DisposeAsset{
    /// <summary>
    /// Represents a request to dispose of an asset.
    /// </summary>
    /// <param name="Reason">
    /// The mandatory reason for disposing of the asset.
    /// </param>
    /// <remarks>
    /// The asset identifier is supplied through the endpoint route.
    /// The performing user and disposal timestamp are determined by the server.
    /// Only application administrators are allowed to perform this operation.
    /// </remarks>
    public sealed record DisposeAssetRequest(
        string Reason);
}