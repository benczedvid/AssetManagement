using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.ValueObjects.Address;

namespace AssetManagement.Application.Vendors.CreateVendor
{
    /// <summary>
    /// Represents a request to create a new vendor.
    /// </summary>
    /// <param name="Name">
    /// The name of the vendor.
    /// </param>
    /// <param name="Address">
    /// The address of the vendor.
    /// </param>
    /// <param name="Webpage">
    /// The webpage of the vendor, when available.
    /// </param>
    public sealed record CreateVendorRequest(
        string Name,
        AddressRequest Address,
        string? Webpage);
}
