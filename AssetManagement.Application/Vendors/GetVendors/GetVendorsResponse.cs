using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Vendors.GetVendors
{
    public sealed record GetVendorsResponse(
        Guid Id,
        string Name,
        AddressResponse Address,
        string? Webpage
        );
}
