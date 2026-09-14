using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Domain.Entities.Vendors;

namespace AssetManagement.Tests.Builders
{
    public sealed class VendorBuilder
    {
        private string? _name = "Tech Supplier Kft.";
        private CountryCodes _countryCode = CountryCodes.HUN;
        private string? _postalCode = "1117";
        private string? _city = "Budapest";
        private string? _street = "Budafoki";
        private PublicSpaces _publicSpace = PublicSpaces.Street;
        private string? _houseNumber = "56";
        private string? _webpage = "https://example.com";

        public VendorBuilder WithName(string? name) { _name = name; return this; }
        public VendorBuilder WithCountryCode(CountryCodes countryCode) { _countryCode = countryCode; return this; }
        public VendorBuilder WithPostalCode(string? postalCode) { _postalCode = postalCode; return this; }
        public VendorBuilder WithCity(string? city) { _city = city; return this; }
        public VendorBuilder WithStreet(string? street) { _street = street; return this; }
        public VendorBuilder WithPublicSpace(PublicSpaces publicSpace) { _publicSpace = publicSpace; return this; }
        public VendorBuilder WithHouseNumber(string? houseNumber) { _houseNumber = houseNumber; return this; }
        public VendorBuilder WithWebpage(string? webpage) { _webpage = webpage; return this; }
        public VendorBuilder WithoutWebpage() { _webpage = null; return this; }

        public Vendor Build()
        {
            return Vendor.Create(
                name: _name!,
                countryCode: _countryCode,
                postalCode: _postalCode!,
                city: _city!,
                street: _street!,
                publicSpace: _publicSpace,
                houseNumber: _houseNumber!,
                webpage: _webpage);
        }
    }
}