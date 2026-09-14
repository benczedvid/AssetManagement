using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.ValueObjects.Address;

namespace AssetManagement.Application.Vendors.CreateVendor
{
    public sealed record CreateVendorRequest(
        string Name,
        AddressRequest Address,
        string? Webpage);
}
