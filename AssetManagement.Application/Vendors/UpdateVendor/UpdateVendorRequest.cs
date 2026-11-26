using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Vendors.UpdateVendor
{
    /// <summary>
    /// Represents a request to update an existing vendor.
    /// Contacts are managed separately: they are standalone entities that can be
    /// attached to or detached from the vendor through the vendor contact endpoints.
    /// </summary>
    /// <param name="Name">
    /// The updated name of the vendor.
    /// </param>
    /// <param name="Address">
    /// The updated address of the vendor.
    /// </param>
    /// <param name="Webpage">
    /// The updated webpage of the vendor, when available.
    /// </param>
    public sealed record UpdateVendorRequest(
        string Name,
        AddressRequest Address,
        string? Webpage
    );
}
