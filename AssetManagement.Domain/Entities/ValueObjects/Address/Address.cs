namespace AssetManagement.Domain.Entities.ValueObjects.Address
{
    public sealed record Address
    {
        public CountryCodes CountryCode { get; }
        public string PostalCode { get; } = string.Empty;
        public string City { get; } = string.Empty;
        public string Street { get; } = string.Empty;
        public PublicSpaces PublicSpace { get; }
        public string HouseNumber { get; } = string.Empty;

        private Address() { }
        private Address(
            CountryCodes countryCode,
            string postalCode,
            string city,
            string street,
            PublicSpaces publicSpace,
            string houseNumber
            )
        {

            CountryCode = countryCode;
            PostalCode = postalCode;
            City = city;
            Street = street;
            PublicSpace = publicSpace;
            HouseNumber = houseNumber;
        }
        public static Address Create(
            CountryCodes countryCode,
            string postalCode,
            string city,
            string street,
            PublicSpaces publicSpace,
            string houseNumber
            )
        {
            ValidateCountryCodes(countryCode);
            ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
            ArgumentException.ThrowIfNullOrWhiteSpace(city);
            ArgumentException.ThrowIfNullOrWhiteSpace(street);
            ValidatePublicSpaces(publicSpace);
            ArgumentException.ThrowIfNullOrWhiteSpace(houseNumber);

            return new Address(
                countryCode: countryCode,
                postalCode: postalCode.Trim(),
                city: city.Trim(),
                street: street.Trim(),
                publicSpace: publicSpace,
                houseNumber: houseNumber.Trim()
                );
        }
        private static void ValidatePublicSpaces(PublicSpaces publicSpace)
        {
            if (publicSpace == PublicSpaces.Unknown || !Enum.IsDefined(publicSpace))
            {
                throw new ArgumentOutOfRangeException(nameof(publicSpace), publicSpace, "A valid public space must be specified.");
            }
        }
        private static void ValidateCountryCodes(CountryCodes countryCode)
        {
            if (countryCode == CountryCodes.Unknown || !Enum.IsDefined(countryCode))
            {
                throw new ArgumentOutOfRangeException(nameof(countryCode), countryCode, "A valid public space must be specified.");
            }
        }
    }
}
