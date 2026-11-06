using AssetManagement.Application.Common.Interfaces.AssetMovements;
using Quartz;

namespace AssetManagement.WebApi.BackgroundJobs
{
    [DisallowConcurrentExecution]
    public sealed class OverdueAssetReportJob : IJob
    {
        private readonly IOverdueAssetReportService _reportService;
        private readonly ILogger<OverdueAssetReportJob> _logger;

        public OverdueAssetReportJob(IOverdueAssetReportService reportService, ILogger<OverdueAssetReportJob> logger)
        {
            ArgumentNullException.ThrowIfNull(reportService);
            ArgumentNullException.ThrowIfNull(logger);

            _reportService = reportService;
            _logger = logger;
        }

        public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.LogInformation(
                "The overdue asset report job started. " +
                "FireInstanceId: {FireInstanceId}, " +
                "ScheduledFireTimeUtc: {ScheduledFireTimeUtc}",
                context.FireInstanceId,
                context.ScheduledFireTimeUtc);

            try
            {
                var result = await _reportService.SendAsync(cancellationToken);

                _logger.LogInformation(
                    "The overdue asset report job completed. " +
                    "StoreCount: {StoreCount}, " +
                    "OverdueAssetCount: {OverdueAssetCount}, " +
                    "EmailSent: {EmailSent}, " +
                    "GeneratedAtUtc: {GeneratedAtUtc}",
                    result.StoreCount,
                    result.OverdueAssetCount,
                    result.EmailSent,
                    result.GeneratedAtUtc);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("The overdue asset report job was cancelled. FireInstanceId: {FireInstanceId}", context.FireInstanceId);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "The overdue asset report job failed. FireInstanceId: {FireInstanceId}", context.FireInstanceId);

                throw new JobExecutionException(exception)
                {
                    RefireImmediately = false
                };
            }
        }
    }
}