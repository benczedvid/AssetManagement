using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Vendors.UpdateVendor
{
    public sealed record UpdateVendorRequest(
        string Name,
        AddressRequest Address,
        string? Webpage,
        IReadOnlyCollection<UpdateContactRequest> Contacts
    );
}
