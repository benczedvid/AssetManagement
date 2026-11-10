using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Application.ServiceDesk.GetServiceDeskSyncOperations;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Provides read-only access to ServiceDesk synchronization operations.
/// </summary>
/// <remarks>
/// Combines request creation, request note creation, and request closure
/// outbox entries into a unified administration response model.
///
/// All database queries are executed without entity tracking.
///
/// This repository does not modify synchronization states.
/// </remarks>
public sealed class ServiceDeskSyncOperationsRepository : IServiceDeskSyncOperationsRepository
{
    private readonly AppDbContext _appDbContext;

    public ServiceDeskSyncOperationsRepository(AppDbContext appDbContext)
    {
        ArgumentNullException.ThrowIfNull(appDbContext);

        _appDbContext = appDbContext;
    }

    /// <summary>
    /// Retrieves ServiceDesk synchronization operations
    /// from all three outbox tables.
    /// </summary>
    public async Task<IReadOnlyList<ServiceDeskSyncOperationItem>> GetOperationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Request creation operations.
        var requestCreationOperations = await (
            from outbox in _appDbContext.ServiceDeskRequestOutbox
                .AsNoTracking()

            join serviceCase in _appDbContext.ServiceCases
                .AsNoTracking()
                on outbox.ServiceCaseId equals serviceCase.Id

            select new ServiceDeskSyncOperationItem(
                outbox.Id,
                outbox.ServiceCaseId,
                outbox.ServiceCaseEventId,
                "RequestCreation",
                outbox.Status.ToString(),
                outbox.ProcessingOperationId,
                outbox.ServiceDeskRequestId,
                outbox.CreatedAtUtc,
                outbox.ProcessingStartedAtUtc,
                outbox.ProcessedAtUtc,
                outbox.Error,
                serviceCase.CaseNumber)
        ).ToListAsync(cancellationToken);

        // 2. Request note creation operations.
        var noteCreationOperations = await (
            from outbox in _appDbContext.ServiceDeskNoteOutbox
                .AsNoTracking()

            join serviceCase in _appDbContext.ServiceCases
                .AsNoTracking()
                on outbox.ServiceCaseId equals serviceCase.Id

            select new ServiceDeskSyncOperationItem(
                outbox.Id,
                outbox.ServiceCaseId,
                outbox.ServiceCaseEventId,
                "NoteCreation",
                outbox.Status.ToString(),
                outbox.ProcessingOperationId,
                outbox.ServiceDeskNoteId,
                outbox.CreatedAtUtc,
                outbox.ProcessingStartedAtUtc,
                outbox.ProcessedAtUtc,
                outbox.Error,
                serviceCase.CaseNumber)
        ).ToListAsync(cancellationToken);

        // 3. Request closure operations.
        var requestClosureOperations = await (
            from outbox in _appDbContext.ServiceDeskRequestClosureOutbox
                .AsNoTracking()

            join serviceCase in _appDbContext.ServiceCases
                .AsNoTracking()
                on outbox.ServiceCaseId equals serviceCase.Id

            select new ServiceDeskSyncOperationItem(
                outbox.Id,
                outbox.ServiceCaseId,
                outbox.ServiceCaseEventId,
                "RequestClosure",
                outbox.Status.ToString(),
                outbox.ProcessingOperationId,
                serviceCase.ServiceDeskRequestId,
                outbox.CreatedAtUtc,
                outbox.ProcessingStartedAtUtc,
                outbox.ProcessedAtUtc,
                outbox.Error,
                serviceCase.CaseNumber)
        ).ToListAsync(cancellationToken);

        // Combine and order synchronization operations.
        return requestCreationOperations
            .Concat(noteCreationOperations)
            .Concat(requestClosureOperations)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.OutboxId)
            .ToList();
    }
}