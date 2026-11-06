using AssetManagement.Domain.Entities.ValueObjects.Address;

namespace AssetManagement.Application.Common.Models
{
    /// <summary>
    /// Represents address information used in API requests.
    /// </summary>
    /// <param name="CountryCode">The country code of the address.</param>
    /// <param name="PostalCode">The postal code of the address.</param>
    /// <param name="City">The city of the address.</param>
    /// <param name="Street">The street name of the address.</param>
    /// <param name="PublicSpace">The type of public space, such as street, road, or square.</param>
    /// <param name="HouseNumber">The house number of the address.</param>
    public sealed record AddressRequest(
        CountryCodes CountryCode,
        string PostalCode,
        string City,
        string Street,
        PublicSpaces PublicSpace,
        string HouseNumber
    );
}
