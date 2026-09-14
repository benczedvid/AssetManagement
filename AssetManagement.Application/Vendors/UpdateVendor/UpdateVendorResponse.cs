using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Vendors.UpdateVendor
{
    public sealed record UpdateVendorResponse(
        Guid Id,
        string Name,
        AddressResponse Address,
        string? Webpage,
        IReadOnlyCollection<ContactResponse> Contacts);
}
