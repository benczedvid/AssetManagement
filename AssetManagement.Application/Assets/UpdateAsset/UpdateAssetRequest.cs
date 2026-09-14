using AssetManagement.Domain.Entities.Assets;


namespace AssetManagement.Application.Assets.UpdateAsset
{
    public sealed record UpdateAssetRequest(
        string AssetName,
        AssetType AssetType,
        AssetStatus AssetStatus,
        string Manufacturer,
        string Model,
        string? MacAddress,
        string? WifiMacAddress,
        string? Imei,
        string? OperatingSystem,
        string? OperatingSystemVersion,
        Guid AssignedStoreId,
        Guid? AssignedUserId,
        Guid? AssignedVendorId,
        string? RfIdTagId
        );
}