using AssetManagement.Application.Common.Interfaces.AssetMovements;
using Quartz;

namespace AssetManagement.WebApi.BackgroundJobs
{
    [DisallowConcurrentExecution]
    public sealed class EndOfDayAssetReportJob : IJob
    {
        private readonly IEndOfDayAssetReportService _reportService;
        private readonly ILogger _logger;
        public EndOfDayAssetReportJob(IEndOfDayAssetReportService reportService, ILogger<EndOfDayAssetReportJob> logger)
        {
            ArgumentNullException.ThrowIfNull(reportService, nameof(reportService));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _reportService = reportService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation(
                "The end-of-day asset report job started. " +
                "FireInstanceId: {FireInstanceId}, " +
                "ScheduledFireTimeUtc: {ScheduledFireTimeUtc}",
                context.FireInstanceId,
                context.ScheduledFireTimeUtc);

            try
            {
                var result = await _reportService.SendAsync(context.CancellationToken);
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
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("The end-of-day asset report job was cancelled. FireInstanceId: {FireInstanceId}", context.FireInstanceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The end-of-day asset report job failed. FireInstanceId: {FireInstanceId}", context.FireInstanceId);
                throw new JobExecutionException(ex, refireImmediately: false);
            }
        }
    }
}
