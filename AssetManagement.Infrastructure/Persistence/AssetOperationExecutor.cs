using System.Data;
using System.Data.Common;
using AssetManagement.Application.Common.Interfaces.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.Persistence;

/// <summary>
/// Executes asset-related operations within a SQL Server transaction
/// protected by exclusive application locks.
/// </summary>
/// <remarks>
/// All participating workflows must acquire the same asset locks
/// before loading and validating business entities.
///
/// The operation delegate must save its changes before returning.
/// The executor commits the transaction but does not call SaveChanges.
///
/// Automatic execution retries and existing transactions are not supported.
/// After a failed operation, tracked entities are detached to prevent
/// rolled-back changes from being saved by a later operation.
/// </remarks>
public sealed class AssetOperationExecutor : IAssetOperationExecutor
{
    private const int LockTimeoutMilliseconds = 15000;
    private const int LockCommandTimeoutSeconds = 30;
    private const string LockResourcePrefix = "AssetManagement:Asset:";
    private const string PackageLockResourcePrefix =  "AssetManagement:Package:";
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AssetOperationExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="AssetOperationExecutor"/> class.
    /// </summary>
    /// <param name="dbContext">
    /// The scoped context shared with the participating repositories.
    /// </param>
    /// <param name="logger">
    /// Records failures encountered while rolling back a transaction.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required dependency is null.
    /// </exception>
    public AssetOperationExecutor(
        AppDbContext dbContext,
        ILogger<AssetOperationExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
public Task<TResult> ExecuteAsync<TResult>(
    IReadOnlyCollection<Guid> assetIds,
    Func<CancellationToken, Task<TResult>> operation,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(assetIds);
    ArgumentNullException.ThrowIfNull(operation);

    var orderedAssetIds = NormalizeAssetIds(
        assetIds,
        allowEmpty: false);

    return ExecuteTransactionAsync(
        async (transaction, token) =>
        {
            foreach (var assetId in orderedAssetIds)
            {
                await AcquireLockAsync(
                    $"{LockResourcePrefix}{assetId:N}",
                    transaction,
                    token);
            }

            return await operation(token);
        },
        cancellationToken);
}

/// <inheritdoc />
public Task<TResult> ExecutePackageAsync<TResult>(
    Guid packageId,
    Func<CancellationToken, Task<IReadOnlyCollection<Guid>>> loadAssetIds,
    Func<IReadOnlyCollection<Guid>, CancellationToken, Task<TResult>> operation,
    CancellationToken cancellationToken = default)
{
    if (packageId == Guid.Empty)
    {
        throw new ArgumentException(
            "The package identifier cannot be empty.",
            nameof(packageId));
    }

    ArgumentNullException.ThrowIfNull(loadAssetIds);
    ArgumentNullException.ThrowIfNull(operation);

    return ExecuteTransactionAsync(
        async (transaction, token) =>
        {
            await AcquireLockAsync(
                $"{PackageLockResourcePrefix}{packageId:N}",
                transaction,
                token);

            var assetIds = await loadAssetIds(token);

            if (assetIds is null)
            {
                throw new InvalidOperationException(
                    "The package asset lookup returned a null collection.");
            }

            // Preliminary lookups must not introduce tracked entities.
            EnsureNoTrackedEntities();

            var orderedAssetIds = NormalizeAssetIds(
                assetIds,
                allowEmpty: true);

            foreach (var assetId in orderedAssetIds)
            {
                await AcquireLockAsync(
                    $"{LockResourcePrefix}{assetId:N}",
                    transaction,
                    token);
            }

            return await operation(
                Array.AsReadOnly(orderedAssetIds),
                token);
        },
        cancellationToken);
}

    /// <summary>
    /// Executes a protected operation within a single transaction.
    /// The supplied delegate acquires locks and performs the operation.
    /// </summary>
    private async Task<TResult> ExecuteTransactionAsync<TResult>(
        Func<IDbContextTransaction, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ValidateExecutionContext();

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        try
        {
            var result = await operation(transaction, cancellationToken);

            if (_dbContext.ChangeTracker.HasChanges())
            {
                throw new InvalidOperationException(
                    "The protected operation completed with unsaved changes. " +
                    "Save all changes before returning from the operation.");
            }

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                _logger.LogError(
                    rollbackException,
                    "Failed to roll back the protected operation transaction.");
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }

            throw;
        }
    }

    /// <summary>
    /// Validates, deduplicates, and orders asset identifiers.
    /// </summary>
    private static Guid[] NormalizeAssetIds(
        IReadOnlyCollection<Guid> assetIds,
        bool allowEmpty)
    {
        var orderedAssetIds = assetIds
            .Distinct()
            .OrderBy(assetId => assetId)
            .ToArray();

        if (!allowEmpty && orderedAssetIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one asset identifier is required.",
                nameof(assetIds));
        }

        if (orderedAssetIds.Any(assetId => assetId == Guid.Empty))
        {
            throw new ArgumentException(
                "Asset identifiers cannot be empty.",
                nameof(assetIds));
        }

        return orderedAssetIds;
    }

    /// <summary>
    /// Rejects tracked entities that were loaded before business execution.
    /// </summary>
    private void EnsureNoTrackedEntities()
    {
        var trackedEntries = _dbContext.ChangeTracker
            .Entries()
            .Select(entry =>
                $"{entry.Metadata.ClrType.Name}: {entry.State}")
            .ToArray();

        if (trackedEntries.Length > 0)
        {
            throw new InvalidOperationException(
                "Protected operations require a context with no tracked entities " +
                "before business execution. Tracked entries: " +
                string.Join(", ", trackedEntries) +
                ". Use no-tracking queries for preliminary lookups.");
        }
    }

    /// <summary>
    /// Validates the provider, transaction state, execution strategy,
    /// and change tracker before starting a protected operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the context cannot safely execute the operation.
    /// </exception>
    private void ValidateExecutionContext()
    {
        if (!_dbContext.Database.IsSqlServer())
        {
            throw new InvalidOperationException(
                "Asset operation locking requires SQL Server.");
        }

        if (_dbContext.Database.CurrentTransaction is not null ||
            System.Transactions.Transaction.Current is not null)
        {
            throw new InvalidOperationException(
                "Asset operations cannot run inside an existing transaction.");
        }

        if (_dbContext.Database.CreateExecutionStrategy().RetriesOnFailure)
        {
            throw new InvalidOperationException(
                "Asset operations do not support an automatically retrying " +
                "execution strategy. Configure a non-retrying strategy " +
                "for the scoped AppDbContext used by this executor.");
        }

        EnsureNoTrackedEntities();
    }

    /// <summary>
    /// Acquires an exclusive transaction-owned application lock
    /// for the specified resource.
    /// </summary>
    /// <param name="resourceName">
    /// The fully qualified package or asset lock resource name.
    /// </param>
    /// <param name="transaction">
    /// The transaction that owns the application lock.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lock acquisition.
    /// </param>
    /// <returns>
    /// A task representing asynchronous lock acquisition.
    /// </returns>
    /// <exception cref="TimeoutException">
    /// Thrown when the lock cannot be acquired within the timeout.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when lock acquisition is canceled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when SQL Server cannot grant the application lock.
    /// </exception>
    private async Task AcquireLockAsync(
        string resourceName,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command =
            _dbContext.Database.GetDbConnection().CreateCommand();

        command.Transaction = transaction.GetDbTransaction();
        command.CommandTimeout = LockCommandTimeoutSeconds;

        command.CommandText = """
            DECLARE @result int;

            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = @lockTimeout,
                @DbPrincipal = 'public';

            SELECT @result;
            """;

        AddParameter(
            command,
            "@resource",
            DbType.String,
            resourceName,
            size: 255);

        AddParameter(
            command,
            "@lockTimeout",
            DbType.Int32,
            LockTimeoutMilliseconds);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);

        if (scalar is not int result)
        {
            throw new InvalidOperationException(
                "SQL Server did not return a valid application lock result.");
        }

        switch (result)
        {
            case >= 0:
                return;

            case -1:
                throw new TimeoutException(
                    $"The lock for resource '{resourceName}' could not be " +
                    "acquired within the configured timeout.");

            case -2:
                throw new OperationCanceledException(
                    $"Lock acquisition for resource '{resourceName}' was canceled.",
                    cancellationToken);

            case -3:
                throw new InvalidOperationException(
                    $"Lock acquisition for resource '{resourceName}' " +
                    "was selected as a deadlock victim.");

            default:
                throw new InvalidOperationException(
                    $"Lock acquisition for resource '{resourceName}' failed " +
                    $"with SQL Server return code '{result}'.");
        }
    }

    /// <summary>
    /// Adds a typed parameter to a database command.
    /// </summary>
    /// <param name="command">The target database command.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="type">The database parameter type.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="size">The optional parameter size.</param>
    private static void AddParameter(
        DbCommand command,
        string name,
        DbType type,
        object value,
        int? size = null)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;

        if (size.HasValue)
        {
            parameter.Size = size.Value;
        }

        command.Parameters.Add(parameter);
    }
}