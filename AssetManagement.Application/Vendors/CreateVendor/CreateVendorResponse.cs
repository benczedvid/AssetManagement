using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Vendors.CreateVendor
{
    public sealed record CreateVendorResponse(
        Guid Id,
        string Name,
        AddressResponse Address,
        string? Webpage
        );
}
