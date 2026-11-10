using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.ServiceDesk;

/// <summary>
/// Periodically processes ServiceDesk synchronization operations.
/// </summary>
/// <remarks>
/// Processing order:
/// 1. Request creation.
/// 2. Request note creation.
/// 3. Request closure.
///
/// Each processing cycle uses a new dependency injection scope.
/// External API calls are handled by the corresponding processors.
/// </remarks>
public sealed class ServiceDeskSynchronizationBackgroundService : BackgroundService
{
    private const int DefaultBatchSize = 20;
    private static readonly TimeSpan ProcessingInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ProcessingTimeout =  TimeSpan.FromMinutes(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServiceDeskSynchronizationBackgroundService> _logger;

    public ServiceDeskSynchronizationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ServiceDeskSynchronizationBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ServiceDesk synchronization background service started.");

        using var timer = new PeriodicTimer(ProcessingInterval);

        try
        {
            await ProcessCycleAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessCycleAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Expected during application shutdown.
        }
        finally
        {
            _logger.LogInformation("ServiceDesk synchronization background service stopped.");
        }
    }

    private async Task ProcessCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            // 1. Request creation.
            var requestProcessor = scope.ServiceProvider.GetRequiredService<ServiceDeskRequestOutboxProcessor>();

            await requestProcessor.ReconcileStaleProcessingAsync(
                processingTimeout: ProcessingTimeout,
                batchSize: DefaultBatchSize,
                cancellationToken: cancellationToken);

            await requestProcessor.ProcessPendingAsync(
                batchSize: DefaultBatchSize,
                cancellationToken: cancellationToken);

            // 2. Request note creation.
            var noteProcessor = scope.ServiceProvider.GetRequiredService<ServiceDeskNoteOutboxProcessor>();

            await noteProcessor.ReconcileStaleProcessingAsync(
                processingTimeout: ProcessingTimeout,
                batchSize: DefaultBatchSize,
                cancellationToken: cancellationToken);

            await noteProcessor.ProcessPendingAsync(
                batchSize: DefaultBatchSize,
                cancellationToken: cancellationToken);

            // 3. Request closure.
            var closureProcessor = scope.ServiceProvider.GetRequiredService<ServiceDeskRequestClosureOutboxProcessor>();

            await closureProcessor.ReconcileStaleProcessingAsync(
                processingTimeout: ProcessingTimeout,
                batchSize: DefaultBatchSize,
                cancellationToken: cancellationToken);

            await closureProcessor.ProcessPendingAsync(
                batchSize: DefaultBatchSize,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred during the ServiceDesk synchronization processing cycle.");
        }
    }
}