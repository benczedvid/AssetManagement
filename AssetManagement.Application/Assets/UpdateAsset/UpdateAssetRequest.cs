using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Application.Assets.UpdateAsset
{
    /// <summary>
    /// Represents a request to update an existing asset.
    /// </summary>
    /// <param name="AssetName">The updated name of the asset.</param>
    /// <param name="AssetType">The updated type of the asset.</param>
    /// <param name="AssetStatus">The updated status of the asset.</param>
    /// <param name="Manufacturer">The updated manufacturer of the asset.</param>
    /// <param name="Model">The updated model of the asset.</param>
    /// <param name="MacAddress">The MAC address of the asset, when available.</param>
    /// <param name="WifiMacAddress">The Wi-Fi MAC address of the asset, when available.</param>
    /// <param name="Imei">The IMEI number of the asset, when available.</param>
    /// <param name="OperatingSystem">The operating system installed on the asset, when available.</param>
    /// <param name="OperatingSystemVersion">The version of the installed operating system, when available.</param>
    /// <param name="AssignedStoreId">The unique identifier of the store to which the asset is assigned.</param>
    /// <param name="AssignedUserId">The unique identifier of the application user assigned to the asset, when applicable.</param>
    /// <param name="AssignedVendorId">The unique identifier of the vendor associated with the asset, when applicable.</param>
    /// <param name="RfIdTagId">The RFID tag identifier assigned to the asset, when available.</param>
    /// <param name="WarrantyEndDate">The date on which the asset warranty expires, when available.</param>
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
        string? RfIdTagId,
        DateOnly? WarrantyEndDate
        );
}