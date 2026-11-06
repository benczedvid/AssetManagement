using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.ValueObjects.Address;

namespace AssetManagement.Tests.Builders
{
    public sealed class StoreBuilder
    {
        private string? _storeNumber = "321";
        private string? _name = "Budapest M3";
        private CountryCodes _countryCode = CountryCodes.HUN;
        private string? _postalCode = "1152";
        private string? _city = "Budapest";
        private string? _street = "Városkapu";
        private PublicSpaces _publicSpace = PublicSpaces.Street;
        private string? _houseNumber = "5";

        public StoreBuilder WithStoreNumber(string? storeNumber) { _storeNumber = storeNumber; return this; }
        public StoreBuilder WithName(string? name) { _name = name; return this; }
        public StoreBuilder WithCountryCode(CountryCodes countryCode) { _countryCode = countryCode; return this; }
        public StoreBuilder WithPostalCode(string? postalCode) { _postalCode = postalCode; return this; }
        public StoreBuilder WithCity(string? city) { _city = city; return this; }
        public StoreBuilder WithStreet(string? street) { _street = street; return this; }
        public StoreBuilder WithPublicSpace(PublicSpaces publicSpace) { _publicSpace = publicSpace; return this; }
        public StoreBuilder WithHouseNumber(string? houseNumber) { _houseNumber = houseNumber; return this; }

        public Store Build()
        {
            return Store.Create(
                storeNumber: _storeNumber!,
                name: _name!,
                countryCode: _countryCode,
                postalCode: _postalCode!,
                city: _city!,
                street: _street!,
                publicSpace: _publicSpace,
                houseNumber: _houseNumber!);
        }
    }
}