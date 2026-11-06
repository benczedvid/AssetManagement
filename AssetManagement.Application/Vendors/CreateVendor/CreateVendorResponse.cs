using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Vendors.CreateVendor
{
    /// <summary>
    /// Represents the result of successfully creating a new vendor.
    /// </summary>
    /// <param name="Id">The unique identifier of the created vendor.</param>
    /// <param name="Name">The name of the vendor.</param>
    /// <param name="Address">The complete address of the vendor.</param>
    /// <param name="Webpage">The webpage of the vendor, when available.</param>
    public sealed record CreateVendorResponse(
        Guid Id,
        string Name,
        AddressResponse Address,
        string? Webpage
        );
}
