using AssetManagement.Domain.Entities.Assets;


namespace AssetManagement.Application.Assets.CreateAsset
{
    public sealed record CreateAssetRequest(
        string AssetName,
        AssetType AssetType,
        AssetStatus AssetStatus,
        string Manufacturer,
        string Model,
        string SerialNumber,
        string? MacAddress,
        string? WifimacAddress,
        string? Imei,
        string? OperatingSystem,
        string? OperatingSystemVersion,
        string? AssignedEmployeeNumber,
        Guid AssignedStoreId,
        Guid? AssignedUserId,
        Guid? AssignedVendorId,
        string? RfidTagId
    );
}
