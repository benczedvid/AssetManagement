using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Common.Interfaces.Packages;

/// <summary>
/// Defines persistence operations for package aggregates.
/// </summary>
public interface IPackageRepository
{
    /// <summary>
    /// Adds a new package aggregate to the persistence context.
    /// </summary>
    /// <param name="package">
    /// The package aggregate to add.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task AddAsync(Package package, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a package by its unique identifier without enabling
    /// aggregate modification.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The matching package when found; otherwise, <see langword="null"/>.
    /// </returns>
    Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a package by its unique identifier for modification.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The matching tracked package when found; otherwise,
    /// <see langword="null"/>.
    /// </returns>
    Task<Package?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a package by its business package number.
    /// </summary>
    /// <param name="packageNumber">
    /// The business identifier of the package.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The matching package when found; otherwise, <see langword="null"/>.
    /// </returns>
    Task<Package?> GetByPackageNumberAsync(string packageNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a package by its business package number for modification.
    /// </summary>
    /// <param name="packageNumber">
    /// The business identifier of the package.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The matching tracked package when found; otherwise,
    /// <see langword="null"/>.
    /// </returns>
    Task<Package?> GetForUpdateByPackageNumberAsync(string packageNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an asset is already included in an active package.
    /// </summary>
    /// <param name="assetId">
    /// The unique identifier of the asset.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the asset belongs to a package
    /// in the created or in-transit status; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    Task<bool> HasActivePackageForAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets packages visible within the specified store access scope.
    /// </summary>
    /// <param name="storeId">
    /// The optional store identifier used to restrict the results.
    /// When null, packages from all accessible stores may be returned.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A read-only list of matching packages.
    /// </returns>
    Task<IReadOnlyList<Package>> GetListAsync(Guid? storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending package changes.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous save operation.
    /// </returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}