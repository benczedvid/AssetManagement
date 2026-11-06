using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Domain.Entities.ServiceCases;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories;

public sealed class ServiceCaseRepository : IServiceCaseRepository
{
    private readonly AppDbContext _appDbContext;

    public ServiceCaseRepository(AppDbContext appDbContext)
    {
        ArgumentNullException.ThrowIfNull(appDbContext);

        _appDbContext = appDbContext;
    }

    public async Task AddAsync(ServiceCase serviceCase, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);

        await _appDbContext.ServiceCases.AddAsync(serviceCase, cancellationToken);
    }

    public async Task AddEventAsync(ServiceCaseEvent serviceCaseEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceCaseEvent);

        await _appDbContext.ServiceCaseEvents.AddAsync(serviceCaseEvent, cancellationToken);
    }

    public async Task<ServiceCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        return await _appDbContext.ServiceCases
            .AsNoTracking()
            .FirstOrDefaultAsync(serviceCase => serviceCase.Id == id, cancellationToken);
    }

    public async Task<ServiceCase?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id, nameof(id));

        return await _appDbContext.ServiceCases
            .FirstOrDefaultAsync(serviceCase => serviceCase.Id == id, cancellationToken);
    }

    public async Task<ServiceCase?> GetActiveByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(assetId, nameof(assetId));

        return await _appDbContext.ServiceCases
            .AsNoTracking()
            .Where(
                serviceCase =>
                    serviceCase.AssetId == assetId &&
                    serviceCase.Status != ServiceCaseStatus.Closed &&
                    serviceCase.Status != ServiceCaseStatus.Cancelled)
            .OrderByDescending(serviceCase => serviceCase.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasActiveServiceCaseForAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(assetId, nameof(assetId));

        return await _appDbContext.ServiceCases
            .AnyAsync(
                serviceCase =>
                    serviceCase.AssetId == assetId && 
                    serviceCase.Status != ServiceCaseStatus.Closed && 
                    serviceCase.Status != ServiceCaseStatus.Cancelled,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCaseEvent>>GetEventsByServiceCaseIdAsync( Guid serviceCaseId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(serviceCaseId, nameof(serviceCaseId));

        return await _appDbContext.ServiceCaseEvents
            .AsNoTracking()
            .Where(serviceCaseEvent => serviceCaseEvent.ServiceCaseId == serviceCaseId)
            .OrderBy(serviceCaseEvent => serviceCaseEvent.OccurredAtUtc)
            .ThenBy(serviceCaseEvent => serviceCaseEvent.Id)
            .ToListAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<ServiceCase>> GetListAsync(Guid? originStoreId, CancellationToken cancellationToken = default)
    {
        if (originStoreId == Guid.Empty)
        {
            throw new ArgumentException("The origin store identifier cannot be empty.", nameof(originStoreId));
        }

        var query = _appDbContext.ServiceCases.AsNoTracking();

        if (originStoreId.HasValue)
        {
            query = query.Where(serviceCase => serviceCase.OriginStoreId == originStoreId.Value);
        }

        return await query
            .OrderByDescending(serviceCase => serviceCase.CreatedAtUtc)
            .ThenBy(serviceCase => serviceCase.Id)
            .ToListAsync(cancellationToken);
    }
    public async Task<ServiceCase?>GetActiveForUpdateByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(assetId, nameof(assetId));

        return await _appDbContext.ServiceCases
            .Where(serviceCase =>
                serviceCase.AssetId == assetId &&
                serviceCase.Status != ServiceCaseStatus.Closed &&
                serviceCase.Status != ServiceCaseStatus.Cancelled)
            .OrderByDescending(serviceCase => serviceCase.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _appDbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }
}