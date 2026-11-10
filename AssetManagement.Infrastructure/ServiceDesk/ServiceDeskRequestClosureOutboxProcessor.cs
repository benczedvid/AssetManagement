using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.ServiceDesk;

/// <summary>
/// Processes pending ServiceDesk request closure operations.
/// </summary>
/// <remarks>
/// A ServiceDesk request can only be closed after:
/// - the associated service case is Closed,
/// - the external ServiceDesk request exists,
/// - request synchronization has completed,
/// - all related ServiceDesk tasks have completed synchronization.
///
/// Each closure operation is atomically reserved before
/// calling the external ServiceDesk Plus API.
///
/// External HTTP operations are not executed inside
/// database transactions.
///
/// Uncertain outcomes require reconciliation and are
/// never automatically retried.
/// </remarks>
public sealed class ServiceDeskRequestClosureOutboxProcessor
{
    private const int DefaultBatchSize = 20;
    private const int MaximumBatchSize = 100;

    private const string ReconciliationRequiredMessage =
        "The ServiceDesk request closure result could not be confirmed.";
        private const string ProcessingTimeoutMessage =
    "Request closure processing exceeded the configured timeout. The external closure result must be verified.";

    private readonly IServiceDeskRequestClosureOutboxRepository _closureOutboxRepository;

    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IServiceDeskClient _serviceDeskClient;

    private readonly ILogger<ServiceDeskRequestClosureOutboxProcessor>
        _logger;

    public ServiceDeskRequestClosureOutboxProcessor(
        IServiceDeskRequestClosureOutboxRepository closureOutboxRepository,
        IServiceCaseRepository serviceCaseRepository,
        IServiceDeskClient serviceDeskClient,
        ILogger<ServiceDeskRequestClosureOutboxProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(closureOutboxRepository);
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(serviceDeskClient);
        ArgumentNullException.ThrowIfNull(logger);

        _closureOutboxRepository = closureOutboxRepository;
        _serviceCaseRepository = serviceCaseRepository;
        _serviceDeskClient = serviceDeskClient;
        _logger = logger;
    }

    /// <summary>
    /// Processes a batch of pending ServiceDesk request closure entries.
    /// </summary>
    /// <param name="batchSize">
    /// Maximum number of entries to process.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    public async Task ProcessPendingAsync(int batchSize = DefaultBatchSize, CancellationToken cancellationToken = default)
    {
        ValidateBatchSize(batchSize);

        var pendingIds = await _closureOutboxRepository.GetPendingIdsAsync(batchSize, cancellationToken);

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
                    "Unexpected error while processing ServiceDesk request closure outbox entry {OutboxId}.",
                    outboxId);
            }
        }
    }

    /// <summary>
    /// Processes a single ServiceDesk request closure operation.
    /// </summary>
    private async Task ProcessEntryAsync(Guid outboxId, CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;

        // The repository atomically verifies:
        // - the closure entry is Pending,
        // - the service case is Closed,
        // - the external request exists,
        // - request synchronization is Completed,
        // - all related task outbox entries are Completed.
        var reserved = await _closureOutboxRepository.TryBeginProcessingAsync(
                outboxId,
                operationId,
                startedAtUtc,
                cancellationToken);

        if (!reserved)
        {
            // The closure is not ready or another worker reserved it.
            return;
        }

        _logger.LogInformation(
            "ServiceDesk request closure reserved. " +
            "OutboxId: {OutboxId}, OperationId: {OperationId}",
            outboxId,
            operationId);

        ServiceDeskRequestClosureOutbox? outbox = null;

        try
        {
            outbox = await _closureOutboxRepository.GetByIdAsync(outboxId, cancellationToken);

            if (outbox is null)
            {
                throw new InvalidOperationException("The reserved ServiceDesk request closure outbox entry could not be found.");
            }

            if (outbox.Status != ServiceDeskRequestClosureOutboxStatus.Processing ||
                outbox.ProcessingOperationId != operationId)
            {
                throw new InvalidOperationException("The ServiceDesk request closure reservation is no longer valid.");
            }

            var serviceCase = await _serviceCaseRepository.GetByIdAsync(outbox.ServiceCaseId, cancellationToken);

            if (serviceCase is null)
            {
                throw new InvalidOperationException("The service case associated with the closure operation could not be found.");
            }

            if (serviceCase.Status != ServiceCaseStatus.Closed)
            {
                throw new InvalidOperationException("The associated service case is not closed.");
            }

            if (serviceCase.ServiceDeskSyncStatus != ServiceDeskSyncStatus.Completed ||
                string.IsNullOrWhiteSpace(serviceCase.ServiceDeskRequestId))
            {
                throw new InvalidOperationException(
                    "The external ServiceDesk request is not ready for closure.");
            }

            _logger.LogInformation(
                "Closing external ServiceDesk request. " +
                "OutboxId: {OutboxId}, " +
                "OperationId: {OperationId}, " +
                "ServiceCaseId: {ServiceCaseId}, " +
                "ServiceDeskRequestId: {ServiceDeskRequestId}",
                outboxId,
                operationId,
                outbox.ServiceCaseId,
                serviceCase.ServiceDeskRequestId);

            // Never automatically retry an uncertain external operation.
            await _serviceDeskClient.CloseRequestAsync(serviceDeskRequestId: serviceCase.ServiceDeskRequestId, cancellationToken: cancellationToken);

            // The external request may already be closed at this point.
            // A local failure must never trigger another closure attempt.
            var completed = await _closureOutboxRepository.TryCompleteAsync(
                    outboxId,
                    operationId,
                    DateTime.UtcNow,
                    cancellationToken);

            if (!completed)
            {
                throw new InvalidOperationException("The external ServiceDesk request was closed, but local closure completion failed.");
            }

            _logger.LogInformation(
                "ServiceDesk request closure completed. " +
                "OutboxId: {OutboxId}, " +
                "OperationId: {OperationId}, " +
                "ServiceCaseId: {ServiceCaseId}, " +
                "ServiceDeskRequestId: {ServiceDeskRequestId}",
                outboxId,
                operationId,
                outbox.ServiceCaseId,
                serviceCase.ServiceDeskRequestId);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            await TryMarkRequiresReconciliationAsync(
                outboxId,
                operationId,
                "Request closure processing was cancelled before the external result could be confirmed.");

            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "ServiceDesk request closure processing failed. " +
                "OutboxId: {OutboxId}, " +
                "OperationId: {OperationId}, " +
                "ServiceCaseId: {ServiceCaseId}",
                outboxId,
                operationId,
                outbox?.ServiceCaseId);

            await TryMarkRequiresReconciliationAsync(outboxId, operationId, ReconciliationRequiredMessage);
        }
    }

    /// <summary>
    /// Attempts to persist the reconciliation state.
    /// </summary>
    /// <remarks>
    /// Uses an independent cancellation token because
    /// the original operation may already have been cancelled.
    ///
    /// If persistence fails, the entry may remain Processing.
    /// Such entries must not be automatically retried.
    /// </remarks>
    private async Task TryMarkRequiresReconciliationAsync(Guid outboxId, Guid operationId, string reason)
    {
        try
        {
            var updated = await _closureOutboxRepository
                    .TryRequireReconciliationAsync(
                        outboxId,
                        operationId,
                        reason,
                        CancellationToken.None);

            if (!updated)
            {
                _logger.LogWarning(
                    "Could not mark ServiceDesk request closure " +
                    "outbox entry {OutboxId} as requiring reconciliation. " +
                    "OperationId: {OperationId}",
                    outboxId,
                    operationId);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist reconciliation state for " +
                "ServiceDesk request closure outbox entry {OutboxId}. " +
                "OperationId: {OperationId}",
                outboxId,
                operationId);
        }
    }

    /// <summary>
    /// Marks stale request closure operations as requiring reconciliation.
    /// </summary>
    /// <remarks>
    /// Stale operations are never automatically retried.
    ///
    /// The original ProcessingOperationId is preserved.
    /// </remarks>
    public async Task ReconcileStaleProcessingAsync(TimeSpan processingTimeout, int batchSize = DefaultBatchSize, CancellationToken cancellationToken = default)
    {
        if (processingTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(processingTimeout), "Processing timeout must be greater than zero.");
        }

        ValidateBatchSize(batchSize);

        var olderThanUtc = DateTime.UtcNow.Subtract(processingTimeout);

        var staleIds = await _closureOutboxRepository.GetStaleProcessingIdsAsync(olderThanUtc, batchSize, cancellationToken);

        foreach (var outboxId in staleIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var updated = await _closureOutboxRepository
                        .TryMarkStaleProcessingForReconciliationAsync(
                            outboxId,
                            olderThanUtc,
                            ProcessingTimeoutMessage,
                            cancellationToken);

                if (updated)
                {
                    _logger.LogWarning(
                        "Stale ServiceDesk request closure operation requires reconciliation. OutboxId: {OutboxId}",
                        outboxId);
                }
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
                    "Failed to reconcile stale ServiceDesk request closure outbox entry {OutboxId}.",
                    outboxId);
            }
        }
    }

    /// <summary>
    /// Validates the requested batch size.
    /// </summary>
    private static void ValidateBatchSize(int batchSize)
    {
        if (batchSize <= 0 || batchSize > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), $"Batch size must be between 1 and {MaximumBatchSize}.");
        }
    }
}