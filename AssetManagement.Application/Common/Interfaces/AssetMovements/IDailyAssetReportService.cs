using AssetManagement.Application.AssetMovements.AssetMovementReports;

namespace AssetManagement.Application.Common.Interfaces.AssetMovements
{
    public interface IDailyAssetReportService
    {
        Task<DailyAssetReportResult> SendAsync(CancellationToken cancellationToken);
    }
}
