using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Packages.Exceptions;
using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.AssignCourier;

/// <summary>
/// Handles requests for assigning, changing or removing
/// the courier of a package.
/// </summary>
public sealed class AssignCourierHandler
{
    private const int CommentMaxLength = 2000;

    private readonly IPackageRepository _packageRepository;
    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="AssignCourierHandler"/> class.
    /// </summary>
    /// <param name="packageRepository">
    /// The package repository.
    /// </param>
    /// <param name="applicationUserRepository">
    /// The application user repository.
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
    public AssignCourierHandler(
        IPackageRepository packageRepository,
        IApplicationUserRepository applicationUserRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IUserAccessScopeResolver userAccessScopeResolver,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(applicationUserRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(userAccessScopeResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _packageRepository = packageRepository;
        _applicationUserRepository = applicationUserRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _assetOperationExecutor = assetOperationExecutor;
    }

    /// <summary>
    /// Assigns, changes or removes a package courier within
    /// a transaction protected by package and asset-level locks.
    /// </summary>
    /// <remarks>
    /// The package lock is acquired before loading the package items.
    /// All included assets are subsequently locked in a consistent order.
    ///
    /// The core operation reloads the package and validates its current
    /// state before applying any courier changes.
    /// </remarks>
    /// <param name="packageId">
    /// The identifier of the package.
    /// </param>
    /// <param name="request">
    /// The courier assignment request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The updated courier assignment after the transaction commits.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the package identifier or request values are invalid.
    /// </exception>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when the package is not in the Created status,
    /// its contents have changed, or the courier is invalid.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when a required lock cannot be acquired.
    /// </exception>
    public Task<AssignCourierResponse> HandleAsync(
        Guid packageId,
        AssignCourierRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePackageId(packageId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        return _assetOperationExecutor
            .ExecutePackageAsync<AssignCourierResponse>(
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
    /// Assigns, changes or removes the courier of a package.
    /// </summary>
    /// <param name="packageId">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="request">
    /// The courier assignment request.
    /// </param>
    /// <param name="lockedAssetIds">
    /// The unique identifiers of the assets that have been locked for exclusive access during the operation.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The updated courier assignment.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when an identifier or comment is invalid.
    /// </exception>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageCourierNotFoundException">
    /// Thrown when the selected courier cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when the package or courier is not in a valid state.
    /// </exception>
    /// <exception cref="PackageAccessDeniedException">
    /// Thrown when the current user cannot modify the package.
    /// </exception>
    private async Task<AssignCourierResponse> HandleCoreAsync(
        Guid packageId,
        AssignCourierRequest request,
        IReadOnlyCollection<Guid> lockedAssetIds,
        CancellationToken cancellationToken = default)
    {
        ValidatePackageId(packageId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);
        var package = await _packageRepository.GetForUpdateByIdAsync(packageId, cancellationToken)
            ?? throw new PackageNotFoundException(packageId);

        PackageAuthorization.EnsureCanManageAtSource(currentUser, package);

        if (package.Status != PackageStatus.Created)
        {
            throw new PackageStateConflictException("The courier can only be changed for a created package.");
        }
        ValidateLockedPackageAssets(package, lockedAssetIds);

        var nowUtc = DateTime.UtcNow;

        if (request.CourierUserId.HasValue)
        {
            await AssignOrChangeCourierAsync(
                package,
                request.CourierUserId.Value,
                request.Comment,
                currentUser.Id,
                currentUser.DisplayName,
                nowUtc,
                cancellationToken);
        }
        else
        {
            RemoveCourier(
                package,
                request.Comment,
                currentUser.Id,
                currentUser.DisplayName,
                nowUtc);
        }

        await _packageRepository.SaveChangesAsync(cancellationToken);

        return new AssignCourierResponse(
            PackageId: package.Id,
            PackageNumber: package.PackageNumber,
            PackageStatus: package.Status,
            CourierUserId: package.CourierUserId,
            UpdatedAtUtc: nowUtc);
    }

    private async Task AssignOrChangeCourierAsync(
        Package package,
        Guid courierUserId,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(package);

        var courier = await _applicationUserRepository.GetByIdAsync(courierUserId, cancellationToken)
            ?? throw new PackageCourierNotFoundException(courierUserId);

        if (!courier.IsActive)
        {
            throw new PackageStateConflictException("The selected courier is not an active user.");
        }

        try
        {
            package.AssignCourier(
                courierUserId: courier.Id,
                comment: comment,
                performedByUserId: performedByUserId,
                performedByUserName: performedByUserName,
                occurredAtUtc: occurredAtUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(exception.Message);
        }
    }

    private static void RemoveCourier(
        Package package,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(package);

        try
        {
            package.RemoveCourier(
                comment: comment,
                performedByUserId: performedByUserId,
                performedByUserName: performedByUserName,
                occurredAtUtc: occurredAtUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(exception.Message);
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
         * A store-scoped user may change the courier only while the package is still located at the source store.
         *
         * Destination-store users may view the package but cannot modify its courier before dispatch.
         */
        if (package.SourceStoreId != accessScope.StoreId.Value)
        {
            throw new PackageAccessDeniedException("The current user cannot change the courier of a package created by another store.");
        }
    }

    private static void ValidateRequest(AssignCourierRequest request)
    {
        if (request.CourierUserId.HasValue && request.CourierUserId.Value == Guid.Empty)
        {
            throw new ArgumentException("The courier user identifier cannot be empty.", nameof(request.CourierUserId));
        }

        if (request.Comment is not null && string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new ArgumentException("Comment cannot be empty or whitespace.", nameof(request.Comment));
        }

        if (request.Comment?.Trim().Length > CommentMaxLength)
        {
            throw new ArgumentException($"Comment cannot exceed {CommentMaxLength} characters.", nameof(request.Comment));
        }
    }

    private static void ValidatePackageId(Guid packageId)
    {
        if (packageId == Guid.Empty)
        {
            throw new ArgumentException("The package identifier cannot be empty.", nameof(packageId));
        }
    }
    /// <summary>
    /// Loads the current package asset identifiers without tracking.
    /// </summary>
    /// <remarks>
    /// Called after acquiring the package lock.
    /// This method does not modify or save data.
    /// </remarks>
    /// <param name="packageId">
    /// The identifier of the package.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the lookup.
    /// </param>
    /// <returns>
    /// The distinct asset identifiers included in the package.
    /// </returns>
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
    /// Ensures that the package contains exactly the assets
    /// protected by the acquired locks.
    /// </summary>
    /// <param name="package">
    /// The package loaded inside the protected operation.
    /// </param>
    /// <param name="lockedAssetIds">
    /// The identifiers of the protected assets.
    /// </param>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when package contents differ from the locked asset set.
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
                "The package contents changed before courier assignment " +
                "acquired the required asset locks. " +
                "Reload the package and try again.");
        }
    }
}