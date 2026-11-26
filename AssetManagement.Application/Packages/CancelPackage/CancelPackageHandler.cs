using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Packages.Exceptions;
using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.CancelPackage;

/// <summary>
/// Handles package cancellation requests.
/// </summary>
public sealed class CancelPackageHandler
{
    private const int CommentMaxLength = 2000;

    private readonly IPackageRepository _packageRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="CancelPackageHandler"/> class.
    /// </summary>
    /// <param name="packageRepository">
    /// The package repository.
    /// </param>
    /// <param name="currentApplicationUserResolver">
    /// Resolves the current application user.
    /// </param>
    /// <param name="userAccessScopeResolver">
    /// Resolves the current user's store access scope.
    /// </param>
    /// <param name="assetOperationExecutor">
    /// Executes asset operations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required dependency is null.
    /// </exception>
    public CancelPackageHandler(
        IPackageRepository packageRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IUserAccessScopeResolver userAccessScopeResolver,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(userAccessScopeResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _packageRepository = packageRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _assetOperationExecutor = assetOperationExecutor;
    }

    /// <summary>
    /// Cancels a package within a transaction protected by a package lock
    /// followed by exclusive locks for all included assets.
    /// </summary>
    /// <remarks>
    /// Asset identifiers are loaded using a no-tracking query after
    /// the package lock has been acquired.
    ///
    /// Empty packages can also be canceled.
    ///
    /// The core operation reloads and validates the package, saves
    /// its changes, and returns the result. The executor commits
    /// the transaction before returning to the caller.
    /// </remarks>
    /// <param name="packageId">
    /// The identifier of the package to cancel.
    /// </param>
    /// <param name="request">
    /// The cancellation request containing the mandatory comment.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lookup, lock acquisition, and execution.
    /// </param>
    /// <returns>
    /// The cancellation result after the transaction has been committed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the package identifier or comment is invalid.
    /// </exception>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when package contents are invalid or have changed,
    /// or the package state does not allow cancellation.
    /// </exception>
    /// <exception cref="PackageAccessDeniedException">
    /// Thrown when the current user cannot cancel the package.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when a required lock cannot be acquired within the timeout.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the execution context does not support
    /// the protected operation.
    /// </exception>
    public Task<CancelPackageResponse> HandleAsync(
        Guid packageId,
        CancelPackageRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePackageId(packageId);
        ArgumentNullException.ThrowIfNull(request);

        _ = ValidateAndNormalizeComment(request.Comment);

        return _assetOperationExecutor
            .ExecutePackageAsync<CancelPackageResponse>(
                packageId: packageId,
                loadAssetIds: token =>
                    LoadPackageAssetIdsAsync(packageId, token),
                operation: (lockedAssetIds, token) =>
                    HandleCoreAsync(
                        packageId,
                        request,
                        lockedAssetIds,
                        token),
                cancellationToken: cancellationToken);
    }
    /// <summary>
    /// Loads the current package asset identifiers without tracking.
    /// </summary>
    /// <remarks>
    /// Called by the executor after acquiring the package lock
    /// and before acquiring asset locks.
    ///
    /// This method does not modify or save data.
    /// An empty package returns an empty collection.
    /// </remarks>
    /// <param name="packageId">
    /// The identifier of the protected package.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the lookup.
    /// </param>
    /// <returns>
    /// The distinct identifiers of the assets included in the package.
    /// </returns>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when a package item contains an empty asset identifier.
    /// </exception>
    private async Task<IReadOnlyCollection<Guid>> LoadPackageAssetIdsAsync(
        Guid packageId,
        CancellationToken cancellationToken)
    {
        var packageSnapshot =
            await _packageRepository.GetByIdAsync(
                packageId,
                cancellationToken)
            ?? throw new PackageNotFoundException(packageId);

        var assetIds = packageSnapshot.Items
            .Select(item => item.AssetId)
            .Distinct()
            .ToArray();

        if (assetIds.Any(assetId => assetId == Guid.Empty))
        {
            throw new PackageStateConflictException(
                "The package contains an invalid asset identifier.");
        }

        return assetIds;
    }

    /// <summary>
    /// Cancels a package before dispatch.
    /// </summary>
    /// <param name="packageId">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="request">
    /// The package cancellation request.
    /// </param>
    /// <param name="lockedAssetIds">
    /// The collection of asset identifiers that are locked for the current operation.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The cancelled package information.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the package identifier or cancellation
    /// comment is invalid.
    /// </exception>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when the package is not in a valid state for cancellation.
    /// </exception>
    /// <exception cref="PackageAccessDeniedException">
    /// Thrown when the current user cannot cancel the package.
    /// </exception>
    private async Task<CancelPackageResponse> HandleCoreAsync(
        Guid packageId,
        CancelPackageRequest request,
        IReadOnlyCollection<Guid> lockedAssetIds,
        CancellationToken cancellationToken = default)
    {
        ValidatePackageId(packageId);

        ArgumentNullException.ThrowIfNull(request);

        var normalizedComment = ValidateAndNormalizeComment(request.Comment);
        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

        var package = await _packageRepository.GetForUpdateByIdAsync(packageId, cancellationToken)
            ?? throw new PackageNotFoundException(packageId);

        PackageAuthorization.EnsureCanManageAtSource(currentUser, package);
        ValidatePackageForCancellation(package);
        ValidateLockedPackageAssets(package, lockedAssetIds);

        var cancelledAtUtc = DateTime.UtcNow;

        try
        {
            package.Cancel(
                comment: normalizedComment,
                performedByUserId: currentUser.Id,
                performedByUserName: currentUser.DisplayName,
                cancelledAtUtc: cancelledAtUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(exception.Message);
        }

        await _packageRepository.SaveChangesAsync(cancellationToken);

        return new CancelPackageResponse(
            PackageId: package.Id,
            PackageNumber: package.PackageNumber,
            PackageStatus: package.Status,
            CancelledAtUtc: cancelledAtUtc,
            CancelledByUserId: currentUser.Id,
            CancelledByUserName: currentUser.DisplayName,
            Comment: normalizedComment);
    }

    private static void ValidatePackageForCancellation(Package package)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (package.Status != PackageStatus.Created)
        {
            throw new PackageStateConflictException("Only created packages can be cancelled.");
        }
    }

    private static void ValidatePackageAccess(Package package, UserAccessScope accessScope)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(accessScope);

        if (accessScope.CanAccessAllStores)
        {
            return;
        }

        if (!accessScope.StoreId.HasValue || accessScope.StoreId.Value == Guid.Empty)
        {
            throw new PackageAccessDeniedException("The current user does not have a valid store access scope.");
        }

        /*
         * A created package is still physically located at its source store. Therefore, a store-scoped user may only
         * cancel a package whose source store matches the user's assigned store.
         */
        if (package.SourceStoreId != accessScope.StoreId.Value)
        {
            throw new PackageAccessDeniedException("The current user cannot cancel a package created by another store.");
        }
    }

    private static string ValidateAndNormalizeComment(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("A cancellation comment is required.", nameof(comment));
        }

        var normalizedComment = comment.Trim();

        if (normalizedComment.Length > CommentMaxLength)
        {
            throw new ArgumentException($"Comment cannot exceed {CommentMaxLength} characters.", nameof(comment));
        }

        return normalizedComment;
    }

    private static void ValidatePackageId(Guid packageId)
    {
        if (packageId == Guid.Empty)
        {
            throw new ArgumentException("The package identifier cannot be empty.", nameof(packageId));
        }
    }

    /// <summary>
    /// Ensures that the package contains exactly the assets
    /// protected by the acquired locks.
    /// </summary>
    /// <param name="package">
    /// The package reloaded inside the protected operation.
    /// </param>
    /// <param name="lockedAssetIds">
    /// The identifiers of the protected assets.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required argument is null.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when package contents differ from the locked asset set
    /// or contain duplicate asset entries.
    /// </exception>
    private static void ValidateLockedPackageAssets(
        Package package,
        IReadOnlyCollection<Guid> lockedAssetIds)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(lockedAssetIds);

        var currentAssetIds = package.Items
            .Select(item => item.AssetId)
            .ToArray();

        var lockedAssetSet = lockedAssetIds.ToHashSet();

        if (currentAssetIds.Length != lockedAssetSet.Count ||
            !lockedAssetSet.SetEquals(currentAssetIds))
        {
            throw new PackageStateConflictException(
                "The package contents changed before cancellation acquired " +
                "the required asset locks. Reload the package and try again.");
        }
    }
}