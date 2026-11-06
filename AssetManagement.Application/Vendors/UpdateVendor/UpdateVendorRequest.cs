using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Vendors.UpdateVendor
{
    /// <summary>
    /// Represents a request to update an existing vendor.
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
    /// <param name="Contacts">
    /// The collection of contact information associated with the vendor.
    /// </param>
    public sealed record UpdateVendorRequest(
        string Name,
        AddressRequest Address,
        string? Webpage,
        IReadOnlyCollection<UpdateContactRequest> Contacts
    );
}
