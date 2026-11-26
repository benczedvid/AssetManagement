using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Packages.Exceptions;
using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.RemovePackageAsset;

/// <summary>
/// Handles removing an asset from an existing package.
/// </summary>
/// <remarks>
/// Assets can only be removed while the package is in Created status.
///
/// The operation uses package-first locking followed by exclusive
/// asset-level locks.
///
/// Removing an asset does not modify the asset status or any
/// associated service case status.
/// </remarks>
public sealed class RemovePackageAssetHandler
{
    private readonly IPackageRepository _packageRepository;
    private readonly ICurrentApplicationUserResolver
        _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    /// <param name="packageRepository">
    /// The package repository.
    /// </param>
    /// <param name="currentApplicationUserResolver">
    /// Resolves the current application user.
    /// </param>
    /// <param name="assetOperationExecutor">
    /// Executes package operations with transaction protection
    /// and exclusive asset-level locks.
    /// </param>
    public RemovePackageAssetHandler(
        IPackageRepository packageRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(
            currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _packageRepository = packageRepository;
        _currentApplicationUserResolver =
            currentApplicationUserResolver;
        _assetOperationExecutor = assetOperationExecutor;
    }

    /// <summary>
    /// Removes an asset from an existing package.
    /// </summary>
    /// <param name="packageId">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="assetId">
    /// The unique identifier of the asset to remove.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The updated package information.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an identifier is empty.
    /// </exception>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when the package cannot be modified or
    /// the requested asset is not included in the package.
    /// </exception>
    public Task<RemovePackageAssetResponse> HandleAsync(
        Guid packageId,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(
            packageId,
            nameof(packageId));

        ValidateIdentifier(
            assetId,
            nameof(assetId));

        return _assetOperationExecutor
            .ExecutePackageAsync<RemovePackageAssetResponse>(
                packageId: packageId,

                loadAssetIds: token =>
                    LoadPackageAssetIdsAsync(
                        packageId,
                        token),

                operation: (lockedAssetIds, token) =>
                    HandleCoreAsync(
                        packageId,
                        assetId,
                        lockedAssetIds,
                        token),

                cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads the current package asset identifiers
    /// for exclusive locking.
    /// </summary>
    /// <remarks>
    /// This method must perform only no-tracking reads.
    /// It must not modify or save entities.
    /// </remarks>
    private async Task<IReadOnlyCollection<Guid>>
        LoadPackageAssetIdsAsync(
            Guid packageId,
            CancellationToken cancellationToken)
    {
        var package = await _packageRepository
            .GetByIdAsync(
                packageId,
                cancellationToken)
            ?? throw new PackageNotFoundException(packageId);

        return package.Items
            .Select(item => item.AssetId)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Executes the protected package modification.
    /// </summary>
    private async Task<RemovePackageAssetResponse> HandleCoreAsync(
        Guid packageId,
        Guid assetId,
        IReadOnlyCollection<Guid> lockedAssetIds,
        CancellationToken cancellationToken)
    {
        var currentUser = await _currentApplicationUserResolver
            .ResolveAsync(cancellationToken);

        var package = await _packageRepository
            .GetForUpdateByIdAsync(
                packageId,
                cancellationToken)
            ?? throw new PackageNotFoundException(packageId);

        /*
         * Authorization must be checked before modifying
         * the package aggregate.
         */
        PackageAuthorization.EnsureCanManageAtSource(
            currentUser,
            package);

        /*
         * Only Created packages can be edited.
         */
        ValidatePackageStatus(package);

        /*
         * Ensure the requested asset belongs to the package.
         */
        ValidateAssetExistsInPackage(
            package,
            assetId);

        /*
         * Verify that the package contents are protected
         * by the executor's exclusive asset locks.
         */
        ValidateLockedAssets(
            package,
            assetId,
            lockedAssetIds);

        var removedAtUtc = DateTime.UtcNow;

        /*
         * Remove the asset through the existing domain method.
         *
         * Package.RemoveAsset also creates the corresponding
         * AssetRemoved event.
         */
        try
        {
            package.RemoveAsset(
                assetId: assetId,
                performedByUserId: currentUser.Id,
                performedByUserName: currentUser.DisplayName,
                occurredAtUtc: removedAtUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(
                exception.Message);
        }

        /*
         * Persist the aggregate changes.
         *
         * The executor commits the transaction after
         * the operation completes successfully.
         */
        await _packageRepository.SaveChangesAsync(
            cancellationToken);

        return new RemovePackageAssetResponse(
            PackageId: package.Id,
            PackageNumber: package.PackageNumber,
            Status: package.Status,
            AssetId: assetId,
            AssetCount: package.Items.Count,
            RemovedAtUtc: removedAtUtc);
    }

    /// <summary>
    /// Ensures the package is editable.
    /// </summary>
    private static void ValidatePackageStatus(
        Package package)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (package.Status != PackageStatus.Created)
        {
            throw new PackageStateConflictException(
                "Assets can only be removed from a created package.");
        }
    }

    /// <summary>
    /// Ensures the requested asset belongs to the package.
    /// </summary>
    private static void ValidateAssetExistsInPackage(
        Package package,
        Guid assetId)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (!package.Items.Any(
            item => item.AssetId == assetId))
        {
            throw new PackageStateConflictException(
                $"Asset '{assetId}' is not included in package '{package.Id}'.");
        }
    }

    /// <summary>
    /// Ensures all package assets are protected
    /// by exclusive asset locks.
    /// </summary>
    private static void ValidateLockedAssets(
        Package package,
        Guid assetId,
        IReadOnlyCollection<Guid> lockedAssetIds)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(lockedAssetIds);

        var lockedAssetIdSet = lockedAssetIds.ToHashSet();

        if (!lockedAssetIdSet.Contains(assetId))
        {
            throw new PackageStateConflictException(
                "The requested asset is not protected by an exclusive lock.");
        }

        if (package.Items.Any(
            item => !lockedAssetIdSet.Contains(item.AssetId)))
        {
            throw new PackageStateConflictException(
                "The package contents changed during lock acquisition.");
        }
    }

    /// <summary>
    /// Validates a required identifier.
    /// </summary>
    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(
                "Identifier cannot be empty.",
                parameterName);
        }
    }
}