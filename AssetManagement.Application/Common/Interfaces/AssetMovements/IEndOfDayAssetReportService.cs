using AssetManagement.Application.AssetMovements.AssetMovementReports;

namespace AssetManagement.Application.Common.Interfaces.AssetMovements
{
    public interface IEndOfDayAssetReportService
    {
        Task<EndOfDayAssetReportResult> SendAsync(CancellationToken cancellationToken);
    }
}
