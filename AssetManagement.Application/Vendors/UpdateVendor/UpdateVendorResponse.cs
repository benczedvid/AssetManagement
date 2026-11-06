using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Vendors.UpdateVendor
{
    /// <summary>
    /// Represents the result of successfully updating an existing vendor.
    /// </summary>
    /// <param name="Id">The unique identifier of the updated vendor.</param>
    /// <param name="Name">The current name of the vendor.</param>
    /// <param name="Address">The current address of the vendor.</param>
    /// <param name="Webpage">The webpage of the vendor, when available.</param>
    /// <param name="Contacts">The collection of contact persons associated with the vendor.</param>
    public sealed record UpdateVendorResponse(
        Guid Id,
        string Name,
        AddressResponse Address,
        string? Webpage,
        IReadOnlyCollection<ContactResponse> Contacts);
}
