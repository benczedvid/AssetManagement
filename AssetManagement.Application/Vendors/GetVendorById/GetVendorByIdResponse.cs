
using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Vendors.GetVendorById
{
    /// <summary>
    /// Represents detailed information about a vendor returned by the API.
    /// </summary>
    /// <param name="Id">The unique identifier of the vendor.</param>
    /// <param name="Name">The name of the vendor.</param>
    /// <param name="Address">The complete address of the vendor.</param>
    /// <param name="Webpage">The webpage of the vendor, when available.</param>
    /// <param name="Contacts">The collection of contact persons associated with the vendor.</param>
    public sealed record class GetVendorByIdResponse(
        Guid Id,
        string Name,
        AddressResponse Address,
        string? Webpage,
        IReadOnlyCollection<ContactResponse> Contacts
        );
}
