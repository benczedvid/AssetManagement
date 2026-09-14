
using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Vendors.GetVendorById
{
    public sealed record class GetVendorByIdResponse(
        Guid Id,
        string Name,
        AddressResponse Address,
        string? Webpage,
        IReadOnlyCollection<ContactResponse> Contacts
        );
}
