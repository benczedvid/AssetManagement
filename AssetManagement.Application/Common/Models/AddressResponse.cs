using AssetManagement.Domain.Entities.ValueObjects.Address;

namespace AssetManagement.Application.Common.Models
{
    public sealed record AddressResponse
    (
        CountryCodes CountryCode,
        string PostalCode,
        string City,
        string Street,
        PublicSpaces PublicSpace,
        string HouseNumber
        );
}
