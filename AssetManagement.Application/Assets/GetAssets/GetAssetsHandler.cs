using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Employees;
using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.GetAssets
{
    public sealed class GetAssetsHandler
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IUserAccessScopeResolver _userAccessScopeResolver;

        public GetAssetsHandler(
            IAssetRepository assetRepository,
            IEmployeeRepository employeeRepository,
            IUserAccessScopeResolver userAccessScopeResolver)
        {
            ArgumentNullException.ThrowIfNull(assetRepository);
            ArgumentNullException.ThrowIfNull(employeeRepository);
            ArgumentNullException.ThrowIfNull(userAccessScopeResolver);

            _assetRepository = assetRepository;
            _employeeRepository = employeeRepository;
            _userAccessScopeResolver = userAccessScopeResolver;
        }

        public async Task<IReadOnlyList<GetAssetsResponse>> HandleAsync(CancellationToken cancellationToken)
        {
            var accessScope = await _userAccessScopeResolver.ResolveAsync(cancellationToken);

            IReadOnlyList<Asset> assets;

            if (accessScope.CanAccessAllStores)
            {
                assets = await _assetRepository.GetAllAsync(cancellationToken);
            }
            else
            {
                if (!accessScope.StoreId.HasValue)
                {
                    throw new InvalidOperationException("The current user does not have a valid store assignment.");
                }

                assets = await _assetRepository.GetAllByStoreIdAsync(accessScope.StoreId.Value, cancellationToken);
            }

            var employeeNumbers = assets
                .Select(asset => asset.AssignedEmployeeNumber)
                .Where(employeeNumber => !string.IsNullOrWhiteSpace(employeeNumber))
                .Select(employeeNumber => employeeNumber!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var employees = employeeNumbers.Length == 0
                ? []
                : await _employeeRepository.GetByEmployeeNumbersAsync(
                    employeeNumbers,
                    cancellationToken);

            var employeeNamesByNumber = employees
                .GroupBy(
                    employee => employee.EmployeeNumber,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().FullName,
                    StringComparer.OrdinalIgnoreCase);

            return assets
                .OrderBy(asset => asset.AssetName)
                .Select(asset => Map(
                    asset,
                    GetAssignedEmployeeName(
                        asset.AssignedEmployeeNumber,
                        employeeNamesByNumber)))
                .ToArray();
        }

        private static string? GetAssignedEmployeeName(
            string? employeeNumber,
            IReadOnlyDictionary<string, string> employeeNamesByNumber)
        {
            if (string.IsNullOrWhiteSpace(employeeNumber))
            {
                return null;
            }

            return employeeNamesByNumber.TryGetValue(
                employeeNumber,
                out var employeeName)
                    ? employeeName
                    : null;
        }

        private static GetAssetsResponse Map(
            Asset asset,
            string? assignedEmployeeName)
        {
            return new GetAssetsResponse(
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
                AssignedUserId: asset.AssignedUserId,
                AssignedEmployeeName: assignedEmployeeName,
                CreatedAtUtc: asset.CreatedAtUtc,
                AssignedVendorId: asset.AssignedVendorId,
                AssignedVendorName: asset.AssignedVendor?.Name,
                RfidTagId: asset.RfidTagId);
        }
    }
}