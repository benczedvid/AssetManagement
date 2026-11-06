using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Domain.Entities.Packages;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Provides persistence operations for package aggregates.
/// </summary>
public sealed class PackageRepository : IPackageRepository
{
    private readonly AppDbContext _appDbContext;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PackageRepository"/> class.
    /// </summary>
    /// <param name="appDbContext">
    /// The application database context.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="appDbContext"/> is null.
    /// </exception>
    public PackageRepository(AppDbContext appDbContext)
    {
        ArgumentNullException.ThrowIfNull(appDbContext);

        _appDbContext = appDbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(Package package, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        await _appDbContext.Packages.AddAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        return await PackageAggregateQuery(asNoTracking: true)
            .SingleOrDefaultAsync(package => package.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Package?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        return await PackageAggregateQuery(asNoTracking: false)
            .SingleOrDefaultAsync(package => package.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Package?> GetByPackageNumberAsync(string packageNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPackageNumber = ValidateAndNormalizePackageNumber(packageNumber);

        return await PackageAggregateQuery(asNoTracking: true)
            .SingleOrDefaultAsync(package => package.PackageNumber == normalizedPackageNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Package?> GetForUpdateByPackageNumberAsync(string packageNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPackageNumber = ValidateAndNormalizePackageNumber(packageNumber);

        return await PackageAggregateQuery(asNoTracking: false)
            .SingleOrDefaultAsync(package => package.PackageNumber == normalizedPackageNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasActivePackageForAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(assetId, nameof(assetId));

        return await _appDbContext.Packages
            .AnyAsync(
                package =>
                    (
                        package.Status == PackageStatus.Created ||
                        package.Status == PackageStatus.InTransit
                    ) &&
                    package.Items.Any( item => item.AssetId == assetId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Package>> GetListAsync(
        Guid? storeId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Package> query = _appDbContext.Packages
            .AsNoTracking()
            .Include(package => package.SourceStore)
            .Include(package => package.DestinationStore)
            .Include(package => package.CourierUser)
            .Include(package => package.Items);

        if (storeId.HasValue)
        {
            query = query.Where(package =>
                package.SourceStoreId == storeId.Value ||
                package.DestinationStoreId == storeId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _appDbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Package> PackageAggregateQuery(bool asNoTracking)
    {
        IQueryable<Package> query = _appDbContext.Packages
            .Include(package => package.SourceStore)
            .Include(package => package.DestinationStore)
            .Include(package => package.CourierUser)
            .Include(package => package.Items)
            .Include(package => package.Events);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.AsSplitQuery();
    }

    private static void ValidateIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }

    private static void ValidateOptionalIdentifier(Guid? identifier, string parameterName)
    {
        if (identifier.HasValue && identifier.Value == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }

    private static string ValidateAndNormalizePackageNumber(string packageNumber)
    {
        if (string.IsNullOrWhiteSpace(packageNumber))
        {
            throw new ArgumentException("The package number cannot be empty or whitespace.", nameof(packageNumber));
        }

        return packageNumber.Trim();
    }
}