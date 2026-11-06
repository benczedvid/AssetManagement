namespace AssetManagement.Application.Assets.DisposeAsset{

    /// <summary>
    /// Represents an authorization failure when attempting to dispose of an asset.
    /// </summary>
    /// <remarks>
    /// This exception indicates that the current application user is inactive
    /// or does not have the application administrator role.
    /// </remarks>
    public sealed class AssetDisposalAccessDeniedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="AssetDisposalAccessDeniedException"/> class.
        /// </summary>
        public AssetDisposalAccessDeniedException()
            : base("Only active application administrators can dispose of assets.")
        {
        }
    }
}