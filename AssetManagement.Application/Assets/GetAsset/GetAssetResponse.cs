using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.GetAsset
{
    public sealed record GetAssetResponse(
        Guid AssetId,
        string AssetName,
        AssetType AssetType,
        AssetStatus AssetStatus,
        string Manufacturer,
        string Model,
        string SerialNumber,
        DateTime CreatedAtUtc,
        string? MacAddress,
        string? WifiMacAddress,
        string? Imei,
        string? OperatingSystem,
        string? OperatingSystemVersion,
        Guid AssignedStoreId,
        Guid? AssignedUserId,
        Guid? AssignedVendorId,
        string? RfidTagId
        );
}
