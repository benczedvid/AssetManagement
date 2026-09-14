using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.CreateAsset
{
    public sealed record CreateAssetResponse(
        Guid AssetId,
        string AssetName,
        AssetType AssetType,
        AssetStatus AssetStatus,
        string Manufacturer,
        string Model,
        string SerialNumber,
        DateTime CreatedAtUtc,
        string? MacAddress,
        string? WifimacAddress,
        string? Imei,
        string? OperatingSystem,
        string? OperatingSystemVersion,
        string? AssignedEmployeeNumber,
        Guid AssignedStoreId,
        Guid? AssignedUserId,
        Guid? AssignedVendorId,
        string? RfidTagId);
}
