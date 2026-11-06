namespace AssetManagement.Application.Assets.DisposeAsset{

    /// <summary>
    /// Represents a conflict between an asset disposal request
    /// and the current asset or service case state.
    /// </summary>
    public sealed class AssetDisposalStateConflictException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="AssetDisposalStateConflictException"/> class.
        /// </summary>
        /// <param name="message">
        /// The description of the disposal state conflict.
        /// </param>
        public AssetDisposalStateConflictException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="AssetDisposalStateConflictException"/> class
        /// with the original domain exception.
        /// </summary>
        /// <param name="message">
        /// The description of the disposal state conflict.
        /// </param>
        /// <param name="innerException">
        /// The domain exception that caused the conflict.
        /// </param>
        public AssetDisposalStateConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}