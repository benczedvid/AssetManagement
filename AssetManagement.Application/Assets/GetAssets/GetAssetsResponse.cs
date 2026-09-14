using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.GetAssets
{
    public sealed record GetAssetsResponse
    (
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
        Guid? AssignedUserId,
        string? AssignedEmployeeName,
        DateTime CreatedAtUtc,
        Guid? AssignedVendorId,
        string? AssignedVendorName,
        string? RfidTagId
        );
}
