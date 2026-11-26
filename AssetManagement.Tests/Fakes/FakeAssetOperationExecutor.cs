using AssetManagement.Application.Common.Interfaces.Assets;

namespace AssetManagement.Tests.Fakes
{
    /// <summary>
    /// Runs asset operations inline without a transaction or locks,
    /// recording which assets the caller asked to lock.
    /// </summary>
    public sealed class FakeAssetOperationExecutor : IAssetOperationExecutor
    {
        public int ExecuteCallCount { get; private set; }
        public int ExecutePackageCallCount { get; private set; }
        public IReadOnlyCollection<Guid> LastLockedAssetIds { get; private set; } = Array.Empty<Guid>();
        public Guid? LastLockedPackageId { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public async Task<TResult> ExecuteAsync<TResult>(
            IReadOnlyCollection<Guid> assetIds,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(assetIds);
            ArgumentNullException.ThrowIfNull(operation);

            ExecuteCallCount++;
            LastLockedAssetIds = assetIds.ToArray();
            LastCancellationToken = cancellationToken;

            return await operation(cancellationToken);
        }

        public async Task<TResult> ExecutePackageAsync<TResult>(
            Guid packageId,
            Func<CancellationToken, Task<IReadOnlyCollection<Guid>>> loadAssetIds,
            Func<IReadOnlyCollection<Guid>, CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(loadAssetIds);
            ArgumentNullException.ThrowIfNull(operation);

            ExecutePackageCallCount++;
            LastLockedPackageId = packageId;
            LastCancellationToken = cancellationToken;

            var assetIds = await loadAssetIds(cancellationToken);

            LastLockedAssetIds = assetIds.ToArray();

            return await operation(assetIds, cancellationToken);
        }
    }
}
