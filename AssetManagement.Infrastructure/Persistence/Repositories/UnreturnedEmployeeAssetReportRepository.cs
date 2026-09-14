using AssetManagement.Application.AssetMovements.AssetMovementReports;
using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Domain.Entities.AssetMovements;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories
{
    public sealed class UnreturnedEmployeeAssetReportRepository
        : IUnreturnedEmployeeAssetReportRepository
    {
        private readonly AppDbContext _appDbContext;

        public UnreturnedEmployeeAssetReportRepository(
            AppDbContext appDbContext)
        {
            ArgumentNullException.ThrowIfNull(appDbContext);

            _appDbContext = appDbContext;
        }

        public async Task<IReadOnlyCollection<UnreturnedEmployeeAssetReportRow>>
            GetUnreturnedEmployeeAssetsAsync(
                CancellationToken cancellationToken)
        {
            var latestEmployeeMovementTimes =
                _appDbContext.AssetMovement
                    .AsNoTracking()
                    .Where(movement =>
                        movement.Type == AssetMovementType.Checkout ||
                        movement.Type == AssetMovementType.Return)
                    .GroupBy(movement => movement.AssetId)
                    .Select(group => new
                    {
                        AssetId = group.Key,
                        CreatedAtUtc = group.Max(
                            movement => movement.CreatedAtUtc)
                    });

            var latestEmployeeMovements =
                from movement in _appDbContext.AssetMovement.AsNoTracking()
                join latest in latestEmployeeMovementTimes
                    on new
                    {
                        movement.AssetId,
                        movement.CreatedAtUtc
                    }
                    equals new
                    {
                        latest.AssetId,
                        latest.CreatedAtUtc
                    }
                where movement.Type == AssetMovementType.Checkout
                select movement;

            var reportRows = await (
                from movement in latestEmployeeMovements

                join asset in _appDbContext.Assets.AsNoTracking()
                    on movement.AssetId equals asset.Id

                join employee in _appDbContext.Employees.AsNoTracking()
                    on movement.EmployeeNumber
                    equals employee.EmployeeNumber

                join store in _appDbContext.Stores.AsNoTracking()
                    on employee.StoreNumber
                    equals store.StoreNumber

                orderby
                    store.StoreNumber,
                    employee.FullName,
                    movement.CreatedAtUtc

                select new UnreturnedEmployeeAssetReportRow(
                    AssetMovementId: movement.Id,
                    AssetId: asset.Id,
                    AssetName: asset.AssetName,
                    AssetSerialNumber: asset.SerialNumber,
                    EmployeeNumber: employee.EmployeeNumber,
                    EmployeeFullName: employee.FullName,
                    StoreId: store.Id,
                    StoreNumber: store.StoreNumber,
                    StoreName: store.Name,
                    CheckedOutAtUtc: movement.CreatedAtUtc))
                .ToArrayAsync(cancellationToken);

            return reportRows;
        }
    }
}