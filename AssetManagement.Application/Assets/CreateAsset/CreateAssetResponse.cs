using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.CreateAsset
{
    /// <summary>
    /// Represents the result of a successfully created asset.
    /// </summary>
    /// <param name="AssetId">The unique identifier of the created asset.</param>
    /// <param name="AssetName">The name of the asset.</param>
    /// <param name="AssetType">The type of the asset.</param>
    /// <param name="AssetStatus">The current status of the asset.</param>
    /// <param name="Manufacturer">The manufacturer of the asset.</param>
    /// <param name="Model">The model of the asset.</param>
    /// <param name="SerialNumber">The unique serial number of the asset.</param>
    /// <param name="CreatedAtUtc">The date and time when the asset was created, in UTC.</param>
    /// <param name="MacAddress">The MAC address of the asset, when available.</param>
    /// <param name="WifimacAddress">The Wi-Fi MAC address of the asset, when available.</param>
    /// <param name="Imei">The IMEI number of the asset, when available.</param>
    /// <param name="OperatingSystem">The operating system installed on the asset, when available.</param>
    /// <param name="OperatingSystemVersion">The version of the installed operating system, when available.</param>
    /// <param name="AssignedEmployeeNumber">The employee number of the employee assigned to the asset, when applicable.</param>
    /// <param name="AssignedStoreId">The unique identifier of the store to which the asset is assigned.</param>
    /// <param name="AssignedUserId">The unique identifier of the application user assigned to the asset, when applicable.</param>
    /// <param name="AssignedVendorId">The unique identifier of the vendor associated with the asset, when applicable.</param>
    /// <param name="RfidTagId">The RFID tag identifier assigned to the asset, when available.</param>
    /// <param name="WarrantyEndDate">The date on which the asset warranty expires, when available.</param>
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
        string? RfidTagId,
        DateOnly? WarrantyEndDate);
}
