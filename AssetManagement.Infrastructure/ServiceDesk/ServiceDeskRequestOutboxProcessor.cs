using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Domain.Entities.ServiceDesk;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.ServiceDesk;

/// <summary>
/// Processes pending ServiceDesk request creation operations.
/// </summary>
/// <remarks>
/// Each request creation operation is atomically reserved before
/// calling the external ServiceDesk Plus API.
///
/// External HTTP calls are never executed inside database transactions.
///
/// Uncertain creation results require reconciliation and must not
/// be automatically retried.
/// </remarks>
public sealed class ServiceDeskRequestOutboxProcessor
{
    private const int DefaultBatchSize = 20;
    private const int MaximumBatchSize = 100;

    private const string ReconciliationRequiredMessage = "The ServiceDesk request creation result could not be confirmed.";
    private const string ProcessingTimeoutMessage = "Request creation processing exceeded the configured timeout. The external result must be verified.";

    private readonly IServiceDeskRequestOutboxRepository _outboxRepository;
    private readonly IServiceDeskClient _serviceDeskClient;
    private readonly ILogger<ServiceDeskRequestOutboxProcessor> _logger;
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly IAssetRepository _assetRepository;

    public ServiceDeskRequestOutboxProcessor(
        IServiceDeskRequestOutboxRepository outboxRepository,
        IServiceDeskClient serviceDeskClient,
        ILogger<ServiceDeskRequestOutboxProcessor> logger,
        IServiceCaseRepository serviceCaseRepository,
        IApplicationUserRepository applicationUserRepository,
        IAssetRepository assetRepository)
    {
        ArgumentNullException.ThrowIfNull(outboxRepository);
        ArgumentNullException.ThrowIfNull(serviceDeskClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(applicationUserRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);

        _outboxRepository = outboxRepository;
        _serviceDeskClient = serviceDeskClient;
        _logger = logger;
        _serviceCaseRepository = serviceCaseRepository;
        _applicationUserRepository = applicationUserRepository;
        _assetRepository = assetRepository;
    }

    /// <summary>
    /// Processes a batch of pending ServiceDesk request creation entries.
    /// </summary>
    public async Task ProcessPendingAsync(int batchSize = DefaultBatchSize, CancellationToken cancellationToken = default)
    {
        ValidateBatchSize(batchSize);

        var pendingIds = await _outboxRepository.GetPendingIdsAsync(batchSize, cancellationToken);

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
                    "Unexpected error while processing ServiceDesk request outbox entry {OutboxId}.",
                    outboxId);
            }
        }
    }

    /// <summary>
    /// Processes a single ServiceDesk request creation operation.
    /// </summary>
    /// <remarks>
    /// The outbox entry is atomically reserved before processing.
    ///
    /// The associated service case and its creator are loaded
    /// before calling the external ServiceDesk Plus API.
    ///
    /// External HTTP operations are never executed inside
    /// database transactions.
    ///
    /// Uncertain outcomes require reconciliation and must not
    /// be automatically retried.
    /// </remarks>
    private async Task ProcessEntryAsync(Guid outboxId, CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;

        // Atomically reserve the request creation operation.
        var reserved = await _outboxRepository.TryBeginProcessingAsync(
            outboxId,
            operationId,
            startedAtUtc,
            cancellationToken);

        if (!reserved)
        {
            return;
        }

        _logger.LogInformation(
            "ServiceDesk request creation reserved. " +
            "OutboxId: {OutboxId}, OperationId: {OperationId}",
            outboxId,
            operationId);

        ServiceDeskRequestOutbox? outbox = null;
        var clientCallStarted = false;

        try
        {
            // 1. Load the reserved outbox entry.
            outbox = await _outboxRepository.GetByIdAsync(outboxId, cancellationToken);

            if (outbox is null)
            {
                throw new InvalidOperationException("The reserved ServiceDesk request outbox entry could not be found.");
            }

            // 2. Verify the processing reservation.
            if (outbox.Status != ServiceDeskRequestOutboxStatus.Processing || outbox.ProcessingOperationId != operationId)
            {
                throw new InvalidOperationException("The ServiceDesk request creation reservation is no longer valid.");
            }

            if (!string.IsNullOrWhiteSpace(outbox.ServiceDeskRequestId))
            {
                throw new InvalidOperationException("The ServiceDesk request outbox entry already has an external request identifier.");
            }

            // 3. Load the associated service case.
            var serviceCase = await _serviceCaseRepository.GetByIdAsync(outbox.ServiceCaseId, cancellationToken);

            if (serviceCase is null)
            {
                throw new InvalidOperationException("The service case associated with the ServiceDesk request outbox entry was not found.");
            }
            var asset = await _assetRepository.GetByIdAsync(
                serviceCase.AssetId,
                cancellationToken);

            if (asset is null)
            {
                throw new InvalidOperationException(
                    "The asset associated with the service case was not found.");
            }

            // 4. Resolve the user who created the service case.
            var requester = await _applicationUserRepository.GetByIdAsync(serviceCase.CreatedByUserId, cancellationToken);

            if (requester is null)
            {
                throw new InvalidOperationException("The user who created the service case could not be found.");
            }

            if (string.IsNullOrWhiteSpace(requester.DisplayName))
            {
                throw new InvalidOperationException("The ServiceDesk requester display name cannot be empty.");
            }

            _logger.LogInformation(
                "Creating external ServiceDesk request. " +
                "OutboxId: {OutboxId}, " +
                "OperationId: {OperationId}, " +
                "ServiceCaseId: {ServiceCaseId}",
                outboxId,
                operationId,
                outbox.ServiceCaseId);

            // Never automatically retry this POST operation.
            clientCallStarted = true;
            
            // 5. Create the external ServiceDesk request.
            var serviceDeskRequestId = await _serviceDeskClient.CreateRequestAsync(
                    subject: outbox.Subject,
                    requester: requester,
                    description: outbox.Description,
                    assetType: asset.AssetType,
                    cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(serviceDeskRequestId))
            {
                throw new InvalidOperationException("ServiceDesk Plus returned an empty request identifier.");
            }

            var normalizedRequestId = serviceDeskRequestId.Trim();

            // 6. Persist the external request identifier.
            // The external request may already exist at this point.
            // Never issue another POST if local completion fails.
            var completed = await _outboxRepository.TryCompleteAsync(
                outboxId,
                operationId,
                normalizedRequestId,
                DateTime.UtcNow,
                cancellationToken);

            if (!completed)
            {
                throw new InvalidOperationException("The external ServiceDesk request was created, but local synchronization completion failed.");
            }

            _logger.LogInformation(
                "ServiceDesk request synchronization completed. " +
                "OutboxId: {OutboxId}, " +
                "OperationId: {OperationId}, " +
                "ServiceCaseId: {ServiceCaseId}, " +
                "ServiceDeskRequestId: {ServiceDeskRequestId}",
                outboxId,
                operationId,
                outbox.ServiceCaseId,
                normalizedRequestId);
        }

        catch (ServiceDeskRequestNotSentException exception)
        {
            _logger.LogError(
                exception,
                "ServiceDesk request creation failed before the creation POST. " +
                "OutboxId: {OutboxId}, OperationId: {OperationId}",
                outboxId,
                operationId);

            await TryMarkFailedBeforeSendAsync(
                outboxId,
                operationId,
                "The creation POST was not invoked. " +
                (exception.InnerException?.Message ?? exception.Message));

            // The client wraps cancellation occurring before the POST.
            // Preserve host shutdown cancellation after persisting the state.
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            if (clientCallStarted)
            {
                await TryMarkRequiresReconciliationAsync(
                   outboxId,
                    operationId,
                    "Request creation processing was cancelled before the external result could be confirmed.");
            }
            else
            {
                await TryMarkFailedBeforeSendAsync(
                    outboxId,
                    operationId,
                    "Request creation processing was cancelled before the ServiceDesk client was called.");
            }

            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "ServiceDesk request creation failed. " +
                "OutboxId: {OutboxId}, " +
                "OperationId: {OperationId}, " +
                "ServiceCaseId: {ServiceCaseId}",
                outboxId,
                operationId,
                outbox?.ServiceCaseId);

            if (clientCallStarted)
            {
                await TryMarkRequiresReconciliationAsync(outboxId, operationId, ReconciliationRequiredMessage);
            }
            else
            {
                await TryMarkFailedBeforeSendAsync(
                    outboxId,
                    operationId,
                    "The ServiceDesk client was not called. " +
                    exception.Message);
            }
        }
    }

    /// <summary>
    /// Attempts to persist the reconciliation state
    /// using an independent cancellation token.
    /// </summary>
    private async Task TryMarkRequiresReconciliationAsync(Guid outboxId, Guid operationId, string reason)
    {
        try
        {
            var updated =  await _outboxRepository.TryRequireReconciliationAsync(
                    outboxId,
                    operationId,
                    reason,
                    CancellationToken.None);

            if (!updated)
            {
                _logger.LogWarning(
                    "Could not mark ServiceDesk request outbox entry " +
                    "{OutboxId} as requiring reconciliation. " +
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
                "ServiceDesk request outbox entry {OutboxId}. " +
                "OperationId: {OperationId}",
                outboxId,
                operationId);
        }
    }

    private async Task TryMarkFailedBeforeSendAsync(Guid outboxId, Guid operationId, string reason)
    {
        try
        {
            var updated = await _outboxRepository.TryFailBeforeSendAsync(
                outboxId,
                operationId,
                reason,
                DateTime.UtcNow,
                CancellationToken.None);

            if (!updated)
            {
                _logger.LogWarning(
                    "Could not mark ServiceDesk request outbox entry " +
                    "{OutboxId} as failed before sending. " +
                    "OperationId: {OperationId}",
                    outboxId,
                    operationId);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist the pre-send failure for " +
                "ServiceDesk request outbox entry {OutboxId}. " +
                "OperationId: {OperationId}",
                outboxId,
                operationId);
        }
    }

    /// <summary>
    /// Marks stale request creation operations as requiring reconciliation.
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

        var staleIds = await _outboxRepository.GetStaleProcessingIdsAsync(olderThanUtc, batchSize, cancellationToken);

        foreach (var outboxId in staleIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var updated = await _outboxRepository
                        .TryMarkStaleProcessingForReconciliationAsync(
                            outboxId,
                            olderThanUtc,
                            ProcessingTimeoutMessage,
                            cancellationToken);

                if (updated)
                {
                    _logger.LogWarning(
                        "Stale ServiceDesk request creation operation requires reconciliation. OutboxId: {OutboxId}",
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
                    "Failed to reconcile stale ServiceDesk request outbox entry {OutboxId}.",
                    outboxId);
            }
        }
    }

    private static void ValidateBatchSize(int batchSize)
    {
        if (batchSize <= 0 || batchSize > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), $"Batch size must be between 1 and {MaximumBatchSize}.");
        }
    }
}