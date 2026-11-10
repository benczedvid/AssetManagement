using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
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

    public async Task<bool> TryBeginServiceDeskSyncAsync(Guid serviceCaseId, Guid operationId, DateTime startedAtUtc, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(serviceCaseId, nameof(serviceCaseId));
        ValidateIdentifier(operationId, nameof(operationId));

        var affectedRows = await _appDbContext.ServiceCases
            .Where(serviceCase =>
                serviceCase.Id == serviceCaseId &&
                serviceCase.ServiceDeskRequestId == null &&
                serviceCase.ServiceDeskSyncStatus == ServiceDeskSyncStatus.NotStarted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncStatus,
                        ServiceDeskSyncStatus.Pending)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskOperationId,
                        operationId)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncStartedAtUtc,
                        startedAtUtc)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncCompletedAtUtc,
                        (DateTime?)null)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncError,
                        (string?)null),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryCompleteServiceDeskSyncAsync(
        Guid serviceCaseId,
        Guid operationId,
        string serviceDeskRequestId,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(serviceCaseId, nameof(serviceCaseId));
        ValidateIdentifier(operationId, nameof(operationId));

        if (string.IsNullOrWhiteSpace(serviceDeskRequestId))
        {
            throw new ArgumentException("The ServiceDesk request identifier cannot be empty.", nameof(serviceDeskRequestId));
        }

        var normalizedRequestId = serviceDeskRequestId.Trim();

        if (normalizedRequestId.Length > 100)
        {
            throw new ArgumentException("The ServiceDesk request identifier cannot exceed 100 characters.", nameof(serviceDeskRequestId));
        }

        var affectedRows = await _appDbContext.ServiceCases
            .Where(serviceCase =>
                serviceCase.Id == serviceCaseId &&
                serviceCase.ServiceDeskOperationId == operationId &&
                serviceCase.ServiceDeskSyncStatus ==  ServiceDeskSyncStatus.Pending &&
                serviceCase.ServiceDeskRequestId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskRequestId,
                        normalizedRequestId)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncStatus,
                        ServiceDeskSyncStatus.Completed)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncCompletedAtUtc,
                        completedAtUtc)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncError,
                        (string?)null),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryFailServiceDeskSyncAsync(Guid serviceCaseId, Guid operationId, string error, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(serviceCaseId, nameof(serviceCaseId));
        ValidateIdentifier(operationId, nameof(operationId));

        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("The ServiceDesk error cannot be empty.", nameof(error));
        }

        var normalizedError = error.Trim();

        if (normalizedError.Length > 1000)
        {
            normalizedError = normalizedError[..1000];
        }

        var affectedRows = await _appDbContext.ServiceCases
            .Where(serviceCase =>
                serviceCase.Id == serviceCaseId &&
                serviceCase.ServiceDeskOperationId == operationId &&
                serviceCase.ServiceDeskSyncStatus == ServiceDeskSyncStatus.Pending &&
                serviceCase.ServiceDeskRequestId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncStatus,
                        ServiceDeskSyncStatus.Failed)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncError,
                        normalizedError),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryRequireServiceDeskReconciliationAsync(Guid serviceCaseId, Guid operationId, string reason, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(serviceCaseId, nameof(serviceCaseId));
        ValidateIdentifier(operationId, nameof(operationId));

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("The reconciliation reason cannot be empty.", nameof(reason));
        }

        var normalizedReason = reason.Trim();

        if (normalizedReason.Length > 1000)
        {
            normalizedReason = normalizedReason[..1000];
        }

        var affectedRows = await _appDbContext.ServiceCases
            .Where(serviceCase =>
                serviceCase.Id == serviceCaseId &&
                serviceCase.ServiceDeskOperationId == operationId &&
                serviceCase.ServiceDeskSyncStatus == ServiceDeskSyncStatus.Pending &&
                serviceCase.ServiceDeskRequestId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncStatus,
                        ServiceDeskSyncStatus.RequiresReconciliation)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncError,
                        normalizedReason),
                cancellationToken);

        return affectedRows == 1;
    }
    /// <summary>
    /// Completes a ServiceDesk reconciliation operation when
    /// the external request has been verified to exist.
    /// </summary>
    /// <remarks>
    /// Updates the service case only when its synchronization
    /// state is RequiresReconciliation and the operation ID matches.
    ///
    /// The update is atomic and does not require SaveChangesAsync.
    /// </remarks>
    public async Task<bool> TryCompleteServiceDeskReconciliationAsync(Guid serviceCaseId, Guid operationId, string serviceDeskRequestId, DateTime completedAtUtc, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(serviceCaseId, nameof(serviceCaseId));
        ValidateIdentifier(operationId, nameof(operationId));

        if (string.IsNullOrWhiteSpace(serviceDeskRequestId))
        {
            throw new ArgumentException("The ServiceDesk request identifier cannot be empty.", nameof(serviceDeskRequestId));
        }

        var normalizedRequestId = serviceDeskRequestId.Trim();

        if (normalizedRequestId.Length > 100)
        {
            throw new ArgumentException("The ServiceDesk request identifier cannot exceed 100 characters.", nameof(serviceDeskRequestId));
        }

        var affectedRows = await _appDbContext.ServiceCases
            .Where(serviceCase =>
                serviceCase.Id == serviceCaseId &&
                serviceCase.ServiceDeskOperationId == operationId &&
                serviceCase.ServiceDeskSyncStatus == ServiceDeskSyncStatus.RequiresReconciliation &&
                serviceCase.ServiceDeskRequestId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskRequestId,
                        normalizedRequestId)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncStatus,
                        ServiceDeskSyncStatus.Completed)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncCompletedAtUtc,
                        completedAtUtc)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncError,
                        (string?)null),
                cancellationToken);

        return affectedRows == 1;
    }

    /// <summary>
    /// Marks a ServiceDesk reconciliation operation as failed
    /// after confirming that no external request was created.
    /// </summary>
    /// <remarks>
    /// Updates the service case only when its synchronization
    /// state is RequiresReconciliation and the operation ID matches.
    ///
    /// This method does not create or delete external requests.
    /// </remarks>
    public async Task<bool> TryFailServiceDeskReconciliationAsync(Guid serviceCaseId, Guid operationId, string reason, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(serviceCaseId, nameof(serviceCaseId));
        ValidateIdentifier(operationId, nameof(operationId));

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("The reconciliation reason cannot be empty.", nameof(reason));
        }

        var normalizedReason = reason.Trim();

        if (normalizedReason.Length > 1000)
        {
            throw new ArgumentException("The reconciliation reason cannot exceed 1000 characters.", nameof(reason));
        }

        var affectedRows = await _appDbContext.ServiceCases
            .Where(serviceCase =>
                serviceCase.Id == serviceCaseId &&
                serviceCase.ServiceDeskOperationId == operationId &&
                serviceCase.ServiceDeskSyncStatus ==
                    ServiceDeskSyncStatus.RequiresReconciliation &&
                serviceCase.ServiceDeskRequestId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncStatus,
                        ServiceDeskSyncStatus.Failed)
                    .SetProperty(
                        serviceCase => serviceCase.ServiceDeskSyncError,
                        normalizedReason),
                cancellationToken);

        return affectedRows == 1;
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