using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.ServiceDesk;

/// <summary>
/// Processes ServiceDesk Plus request note synchronization operations.
/// </summary>
/// <remarks>
/// Request notes are created outside database transactions.
///
/// Each note operation is reserved atomically before calling
/// the external ServiceDesk Plus API.
///
/// Uncertain external API results require reconciliation and
/// are never automatically retried.
/// </remarks>
public sealed class ServiceDeskNoteOutboxProcessor
{
    private readonly IServiceDeskNoteOutboxRepository _noteOutboxRepository;
    private readonly IServiceDeskClient _serviceDeskClient;
    private readonly ILogger<ServiceDeskNoteOutboxProcessor> _logger;
    private readonly IServiceCaseRepository _serviceCaseRepository;

    public ServiceDeskNoteOutboxProcessor(
        IServiceDeskNoteOutboxRepository noteOutboxRepository,
        IServiceDeskClient serviceDeskClient,
        ILogger<ServiceDeskNoteOutboxProcessor> logger,
        IServiceCaseRepository serviceCaseRepository)
    {
        ArgumentNullException.ThrowIfNull(noteOutboxRepository);
        ArgumentNullException.ThrowIfNull(serviceDeskClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);

        _noteOutboxRepository = noteOutboxRepository;
        _serviceDeskClient = serviceDeskClient;
        _logger = logger;
        _serviceCaseRepository = serviceCaseRepository;
    }

    /// <summary>
    /// Processes pending request note creation operations.
    /// </summary>
    public async Task ProcessPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        ValidateBatchSize(batchSize);

        var pendingIds = await _noteOutboxRepository.GetPendingIdsAsync(batchSize, cancellationToken);

        foreach (var outboxId in pendingIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ProcessEntryAsync(outboxId, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected error processing ServiceDesk note outbox entry. OutboxId: {OutboxId}",
                    outboxId);
            }
        }
    }

    /// <summary>
    /// Reconciles stale processing operations.
    /// </summary>
    /// <remarks>
    /// Stale operations are marked as requiring reconciliation.
    /// They are never automatically returned to Pending.
    /// </remarks>
    public async Task ReconcileStaleProcessingAsync(TimeSpan processingTimeout, int batchSize, CancellationToken cancellationToken = default)
    {
        if (processingTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(processingTimeout),
                "The processing timeout must be greater than zero.");
        }

        ValidateBatchSize(batchSize);

        var olderThanUtc = DateTime.UtcNow.Subtract(processingTimeout);

        var staleIds = await _noteOutboxRepository.GetStaleProcessingIdsAsync(olderThanUtc, batchSize, cancellationToken);

        foreach (var outboxId in staleIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var updated = await _noteOutboxRepository
                .TryMarkStaleProcessingForReconciliationAsync(
                    outboxId,
                    olderThanUtc,
                    "The ServiceDesk request note operation exceeded the processing timeout. " +
                    "The external result must be verified before further action.",
                    cancellationToken);

            if (updated)
            {
                _logger.LogWarning(
                    "Stale ServiceDesk note operation requires reconciliation. OutboxId: {OutboxId}",
                    outboxId);
            }
        }
    }

    /// <summary>
    /// Processes a single request note creation operation.
    /// </summary>
    private async Task ProcessEntryAsync(Guid outboxId, CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;

        // Atomically reserve the operation.
        var reserved = await _noteOutboxRepository.TryBeginProcessingAsync(
            outboxId,
            operationId,
            startedAtUtc,
            cancellationToken);

        if (!reserved)
        {
            return;
        }

        _logger.LogInformation(
            "ServiceDesk note creation reserved. OutboxId: {OutboxId}, OperationId: {OperationId}",
            outboxId,
            operationId);

        try
        {
            var outbox = await _noteOutboxRepository.GetByIdAsync(outboxId, cancellationToken);

            if (outbox is null)
            {
                throw new InvalidOperationException("The reserved ServiceDesk note outbox entry could not be found.");
            }

            // The ServiceDesk request ID must be resolved before
            // the external note creation API call.
            var serviceDeskRequestId = await ResolveServiceDeskRequestIdAsync(outbox.ServiceCaseId, cancellationToken);

            var serviceDeskNoteId = await _serviceDeskClient.CreateRequestNoteAsync(
                serviceDeskRequestId,
                outbox.Description,
                cancellationToken);

            var completed = await _noteOutboxRepository.TryCompleteAsync(
                outboxId,
                operationId,
                serviceDeskNoteId,
                DateTime.UtcNow,
                cancellationToken);

            if (!completed)
            {
                throw new InvalidOperationException(
                    "The ServiceDesk note was created externally, but the local outbox completion could not be recorded.");
            }

            _logger.LogInformation(
                "ServiceDesk note creation completed. OutboxId: {OutboxId}, ServiceDeskNoteId: {ServiceDeskNoteId}",
                outboxId,
                serviceDeskNoteId);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "ServiceDesk note creation failed. OutboxId: {OutboxId}, OperationId: {OperationId}",
                outboxId,
                operationId);

            var marked = await _noteOutboxRepository
                .TryRequireReconciliationAsync(
                    outboxId,
                    operationId,
                    exception.Message,
                    CancellationToken.None);

            if (!marked)
            {
                _logger.LogCritical(
                    "Failed to mark ServiceDesk note operation for reconciliation. OutboxId: {OutboxId}, OperationId: {OperationId}",
                    outboxId,
                    operationId);
            }
        }
    }

    /// <summary>
    /// Resolves the external ServiceDesk request identifier
    /// associated with the specified service case.
    /// </summary>
    private async Task<string> ResolveServiceDeskRequestIdAsync(Guid serviceCaseId, CancellationToken cancellationToken)
    {
        if (serviceCaseId == Guid.Empty)
        {
                throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
        }

        var serviceCase = await _serviceCaseRepository.GetByIdAsync(serviceCaseId, cancellationToken);

        if (serviceCase is null)
        {
            throw new InvalidOperationException("The service case associated with the ServiceDesk note outbox entry could not be found.");
        }

        if (serviceCase.ServiceDeskSyncStatus != ServiceDeskSyncStatus.Completed)
        {
            throw new InvalidOperationException("The ServiceDesk request synchronization has not completed.");
        }

        if (string.IsNullOrWhiteSpace(serviceCase.ServiceDeskRequestId))
        {
            throw new InvalidOperationException("The service case does not have an external ServiceDesk request identifier.");
        }

        return serviceCase.ServiceDeskRequestId.Trim();
    }

    private static void ValidateBatchSize(int batchSize)
    {
        if (batchSize <= 0 || batchSize > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be between 1 and 100.");
        }
    }
}