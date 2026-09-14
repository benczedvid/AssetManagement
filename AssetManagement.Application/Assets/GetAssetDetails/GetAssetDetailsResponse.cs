using AssetManagement.Domain.Entities.Assets;


namespace AssetManagement.Application.Assets.GetAssetDetails
{
    public sealed record GetAssetDetailsResponse(
        Guid AssetId,
        string AssetName,
        AssetType AssetType,
        AssetStatus AssetStatus,
        string Manufacturer,
        string Model,
        string SerialNumber,
        string? MacAddress,
        string? WifiMacAddress,
        string? Imei,
        string? OperatingSystem,
        string? OperatingSystemVersion,
        Guid AssignedStoreId,
        string AssignedStoreName,
        string? AssignedEmployeeNumber,
        string? AssignedEmployeeName,
        DateTime CreatedAtUtc,
        IReadOnlyList<AssetHistoryItemResponse> History,
        Guid? AssignedVendorId,
        string? AssignedVendorName,
        string? RfidTagId);
}