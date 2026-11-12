using AssetManagement.Application.Common.Interfaces.AssetMovements;
using Microsoft.Extensions.Logging;
using Quartz;

namespace AssetManagement.WebApi.BackgroundJobs
{
    [DisallowConcurrentExecution]
    public sealed class EndOfDayAssetReportJob : IJob
    {
        private readonly IEndOfDayAssetReportService _reportService;
        private readonly ILogger<EndOfDayAssetReportJob> _logger;

        public EndOfDayAssetReportJob(
            IEndOfDayAssetReportService reportService,
            ILogger<EndOfDayAssetReportJob> logger)
        {
            ArgumentNullException.ThrowIfNull(reportService);
            ArgumentNullException.ThrowIfNull(logger);

            _reportService = reportService;
            _logger = logger;
        }

        public async ValueTask Execute(
            IJobExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.LogInformation(
                "The end-of-day asset report job started. " +
                "FireInstanceId: {FireInstanceId}, " +
                "ScheduledFireTimeUtc: {ScheduledFireTimeUtc}",
                context.FireInstanceId,
                context.ScheduledFireTimeUtc);

            try
            {
                var result = await _reportService.SendAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "The end-of-day asset report job completed. " +
                    "StoreCount: {StoreCount}, " +
                    "SentEmailCount: {SentEmailCount}, " +
                    "SkippedStoreCount: {SkippedStoreCount}, " +
                    "UnreturnedAssetCount: {UnreturnedAssetCount}, " +
                    "GeneratedAtUtc: {GeneratedAtUtc}",
                    result.StoreCount,
                    result.SentEmailCount,
                    result.SkippedStoreCount,
                    result.UnreturnedAssetCount,
                    result.GeneratedAtUtc);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "The end-of-day asset report job was cancelled. " +
                    "FireInstanceId: {FireInstanceId}",
                    context.FireInstanceId);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "The end-of-day asset report job failed. " +
                    "FireInstanceId: {FireInstanceId}",
                    context.FireInstanceId);

                throw new JobExecutionException(exception)
                {
                    RefireImmediately = false
                };
            }
        }
    }
}