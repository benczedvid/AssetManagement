using AssetManagement.Application.AssetMovements.AssetMovementReports;

namespace AssetManagement.Application.Common.Interfaces.AssetMovements;

public interface IOverdueAssetReportService
{
    Task<OverdueAssetReportResult> SendAsync(CancellationToken cancellationToken);
}