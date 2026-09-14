using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Employees;
using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Employees;
using AssetManagement.Application.Stores;
using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.GetAssetDetails
{
    public sealed class GetAssetDetailsHandler
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetMovementRepository _assetMovementRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IUserAccessScopeResolver _userAccessScopeResolver;

        public GetAssetDetailsHandler(
            IAssetRepository assetRepository,
            IAssetMovementRepository assetMovementRepository,
            IEmployeeRepository employeeRepository,
            IStoreRepository storeRepository,
            IUserAccessScopeResolver userAccessScopeResolver)
        {
            ArgumentNullException.ThrowIfNull(assetRepository);
            ArgumentNullException.ThrowIfNull(assetMovementRepository);
            ArgumentNullException.ThrowIfNull(employeeRepository);
            ArgumentNullException.ThrowIfNull(storeRepository);
            ArgumentNullException.ThrowIfNull(userAccessScopeResolver);

            _assetRepository = assetRepository;
            _assetMovementRepository = assetMovementRepository;
            _employeeRepository = employeeRepository;
            _storeRepository = storeRepository;
            _userAccessScopeResolver = userAccessScopeResolver;
        }

        public async Task<GetAssetDetailsResponse> HandleAsync(Guid assetId, CancellationToken cancellationToken)
        {
            if (assetId == Guid.Empty)
            {
                throw new ArgumentException("The asset identifier cannot be empty.",  nameof(assetId));
            }

            var accessScope = await _userAccessScopeResolver.ResolveAsync(cancellationToken);

            Asset? asset;

            if (accessScope.CanAccessAllStores)
            {
                asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken);
            }
            else
            {
                if (!accessScope.StoreId.HasValue)
                {
                    throw new InvalidOperationException("The current user does not have a valid store assignment.");
                }

                asset = await _assetRepository.GetByIdAndStoreIdAsync(assetId, accessScope.StoreId.Value, cancellationToken);
            }

            if (asset is null)
            {
                throw new AssetNotFoundException(assetId);
            }

            var store = await _storeRepository.GetByIdAsync(
                asset.AssignedStoreId,
                cancellationToken)
                ?? throw new StoreNotFoundException(
                    asset.AssignedStoreId);

            string? assignedEmployeeName = null;
            string? assignedEmployeeNumber = null;

            if (!string.IsNullOrWhiteSpace(
                    asset.AssignedEmployeeNumber))
            {
                var assignedEmployee =
                    await _employeeRepository
                        .GetByEmployeeNumberAsync(
                            asset.AssignedEmployeeNumber,
                            cancellationToken)
                    ?? throw new EmployeeNotFoundException(
                        asset.AssignedEmployeeNumber);

                assignedEmployeeName = assignedEmployee.FullName;
                assignedEmployeeNumber =
                    assignedEmployee.EmployeeNumber;
            }

            var fromUtc = DateTime.UtcNow.AddDays(-7);

            var movements =
                await _assetMovementRepository
                    .GetRecentByAssetIdAsync(
                        asset.Id,
                        fromUtc,
                        cancellationToken);

            var movementEmployeeNumbers = movements
                .Select(movement => movement.EmployeeNumber)
                .Where(employeeNumber =>
                    !string.IsNullOrWhiteSpace(employeeNumber))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var movementEmployees =
                movementEmployeeNumbers.Length == 0
                    ? []
                    : await _employeeRepository
                        .GetByEmployeeNumbersAsync(
                            movementEmployeeNumbers,
                            cancellationToken);

            var employeeNamesByNumber = movementEmployees
                .GroupBy(
                    employee => employee.EmployeeNumber,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().FullName,
                    StringComparer.OrdinalIgnoreCase);

            var storeIds = movements
                .Select(movement => movement.StoreId)
                .Distinct()
                .ToArray();

            var stores = storeIds.Length == 0
                ? []
                : await _storeRepository.GetByIdsAsync(
                    storeIds,
                    cancellationToken);

            var storeNamesById = stores
                .GroupBy(storeItem => storeItem.Id)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Name);

            var history = movements
                .OrderByDescending(
                    movement => movement.CreatedAtUtc)
                .Select(movement =>
                    new AssetHistoryItemResponse(
                        Id: movement.Id,
                        Type: movement.Type,
                        EmployeeNumber: movement.EmployeeNumber,
                        EmployeeName: GetEmployeeName(
                            movement.EmployeeNumber,
                            employeeNamesByNumber),
                        StoreId: movement.StoreId,
                        StoreName: GetStoreName(
                            movement.StoreId,
                            storeNamesById),
                        CreatedAtUtc: movement.CreatedAtUtc))
                .ToArray();

            return new GetAssetDetailsResponse(
                AssetId: asset.Id,
                AssetName: asset.AssetName,
                AssetType: asset.AssetType,
                AssetStatus: asset.AssetStatus,
                Manufacturer: asset.Manufacturer,
                Model: asset.Model,
                SerialNumber: asset.SerialNumber,
                MacAddress: asset.MacAddress,
                WifiMacAddress: asset.WiFiMacAddress,
                Imei: asset.Imei,
                OperatingSystem: asset.OperatingSystem,
                OperatingSystemVersion: asset.OperatingSystemVersion,
                AssignedStoreId: asset.AssignedStoreId,
                AssignedStoreName: store.Name,
                AssignedEmployeeNumber: assignedEmployeeNumber,
                AssignedEmployeeName: assignedEmployeeName,
                CreatedAtUtc: asset.CreatedAtUtc,
                History: history,
                AssignedVendorId: asset.AssignedVendorId,
                AssignedVendorName: asset.AssignedVendor?.Name,
                RfidTagId: asset.RfidTagId);
        }

        private static string GetEmployeeName(string employeeNumber,IReadOnlyDictionary<string, string>employeeNamesByNumber)
        {
            return employeeNamesByNumber.TryGetValue(employeeNumber,out var employeeName) ? employeeName : "Unknown employee";
        }

        private static string GetStoreName(Guid storeId,IReadOnlyDictionary<Guid, string>storeNamesById)
        {
            return storeNamesById.TryGetValue(storeId, out var storeName) ? storeName : "Unknown store";
        }
    }
}