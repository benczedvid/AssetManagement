namespace AssetManagement.Application.Common.Interfaces.Assets;

/// <summary>
/// Executes an asset-related operation within a transaction
/// protected by exclusive asset-level locks.
/// </summary>
/// <remarks>
/// All participating workflows must use this executor before
/// loading assets and validating related package or service case state.
///
/// Implementations must acquire locks in a consistent asset identifier
/// order and retain them until the transaction ends.
///
/// The operation delegate is responsible for saving its changes.
/// The executor commits the transaction only after the delegate
/// completes successfully.
///
/// Implementations must not automatically retry the delegate.
/// </remarks>
public interface IAssetOperationExecutor
{
    /// <summary>
    /// Acquires exclusive locks for the specified assets and executes
    /// the operation within the protected transaction.
    /// </summary>
    /// <typeparam name="TResult">
    /// The result type returned by the operation.
    /// </typeparam>
    /// <param name="assetIds">
    /// The non-empty collection of asset identifiers to protect.
    /// Duplicate identifiers are treated as a single identifier.
    /// </param>
    /// <param name="operation">
    /// The operation to execute after all locks have been acquired.
    /// Asset loading, business validation, state changes, and saving
    /// must take place inside this delegate.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lock acquisition and execution.
    /// </param>
    /// <returns>
    /// The operation result after the transaction has been committed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the asset identifiers or operation delegate are null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when no asset identifiers are supplied or an identifier
    /// is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when execution is requested within an existing transaction
    /// that the executor does not support.
    /// </exception>
    Task<TResult> ExecuteAsync<TResult>(
        IReadOnlyCollection<Guid> assetIds,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a package operation within a transaction protected
    /// by a package lock followed by exclusive asset-level locks.
    /// </summary>
    /// <typeparam name="TResult">
    /// The result type returned by the operation.
    /// </typeparam>
    /// <param name="packageId">
    /// The identifier of the package to protect.
    /// </param>
    /// <param name="loadAssetIds">
    /// Loads the current asset identifiers after the package lock
    /// has been acquired. This delegate must use no-tracking queries
    /// and must not modify or save data.
    /// An empty collection is allowed.
    /// </param>
    /// <param name="operation">
    /// Executes after the package lock and all asset locks
    /// have been acquired. Receives the protected asset identifiers.
    /// The delegate must reload and validate business entities
    /// and save its changes before returning.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lock acquisition and execution.
    /// </param>
    /// <returns>
    /// The operation result after the transaction has been committed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required delegate is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the package identifier or an asset identifier is empty.
    /// </exception>
    /// <remarks>
    /// All operations that change an existing package or its contents
    /// must follow the same package-first locking protocol.
    /// Automatic retries and nested executor calls are not supported.
    /// </remarks>
    Task<TResult> ExecutePackageAsync<TResult>(
        Guid packageId,
        Func<CancellationToken, Task<IReadOnlyCollection<Guid>>> loadAssetIds,
        Func<IReadOnlyCollection<Guid>, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}