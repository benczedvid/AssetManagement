using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.GetAssets
{
    /// <summary>
    /// Represents summary information about an asset returned by the API.
    /// </summary>
    /// <param name="AssetId">The unique identifier of the asset.</param>
    /// <param name="AssetName">The name of the asset.</param>
    /// <param name="AssetType">The type of the asset.</param>
    /// <param name="AssetStatus">The current status of the asset.</param>
    /// <param name="Manufacturer">The manufacturer of the asset.</param>
    /// <param name="Model">The model of the asset.</param>
    /// <param name="SerialNumber">The unique serial number of the asset.</param>
    /// <param name="MacAddress">The MAC address of the asset, when available.</param>
    /// <param name="WifiMacAddress">The Wi-Fi MAC address of the asset, when available.</param>
    /// <param name="Imei">The IMEI number of the asset, when available.</param>
    /// <param name="OperatingSystem">The operating system installed on the asset, when available.</param>
    /// <param name="OperatingSystemVersion">The version of the installed operating system, when available.</param>
    /// <param name="AssignedStoreId">The unique identifier of the store to which the asset is assigned.</param>
    /// <param name="AssignedStoreNumber">The store number of the store to which the asset is assigned.</param>
    /// <param name="AssignedUserId">The unique identifier of the application user assigned to the asset, when applicable.</param>
    /// <param name="AssignedEmployeeName">The name of the employee assigned to the asset, when available.</param>
    /// <param name="CreatedAtUtc">The date and time when the asset was created, in UTC.</param>
    /// <param name="AssignedVendorId">The unique identifier of the vendor associated with the asset, when applicable.</param>
    /// <param name="AssignedVendorName">The name of the vendor associated with the asset, when available.</param>
    /// <param name="RfidTagId">The RFID tag identifier assigned to the asset, when available.</param>
    /// <param name="WarrantyEndDate">The date on which the asset warranty expires, when available.</param>
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
        string AssignedStoreNumber,
        Guid? AssignedUserId,
        string? AssignedEmployeeName,
        DateTime CreatedAtUtc,
        Guid? AssignedVendorId,
        string? AssignedVendorName,
        string? RfidTagId,
        DateOnly? WarrantyEndDate
        );
}
