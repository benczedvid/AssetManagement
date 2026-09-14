using AssetManagement.Application.AssetMovements.AssetMovementReports;

namespace AssetManagement.Application.Common.Interfaces.AssetMovements
{
    public interface IUnreturnedEmployeeAssetReportRepository
    {
        Task<IReadOnlyCollection<UnreturnedEmployeeAssetReportRow>> GetUnreturnedEmployeeAssetsAsync(CancellationToken cancellationToken);
    }
}
