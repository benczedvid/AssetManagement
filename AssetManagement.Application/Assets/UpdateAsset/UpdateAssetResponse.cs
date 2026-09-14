using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Vendors;

namespace AssetManagement.Application.Assets.UpdateAsset
{
    public sealed record UpdateAssetResponse(
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
        Guid? AssignedVendorId,
        string? RfidTagId
        );
}