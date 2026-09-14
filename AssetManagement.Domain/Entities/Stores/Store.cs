using AssetManagement.Domain.Entities.ValueObjects.Address;

namespace AssetManagement.Domain.Entities.Stores;

public sealed class Store
{
    public Guid Id { get; private set; }

    public string StoreNumber { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public Address Address { get; private set; } = null!;

    private Store()
    {
    }

    private Store(
        Guid id,
        string storeNumber,
        string name,
        Address address)
    {
        Id = id;
        StoreNumber = storeNumber;
        Name = name;
        Address = address;
    }

    public static Store Create(
        string storeNumber,
        string name,
        CountryCodes countryCode,
        string postalCode,
        string city,
        string street,
        PublicSpaces publicSpace,
        string houseNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var address = Address.Create(
            countryCode: countryCode,
            postalCode: postalCode,
            city: city,
            street: street,
            publicSpace: publicSpace,
            houseNumber: houseNumber);

        return new Store(
            id: Guid.NewGuid(),
            storeNumber: storeNumber.Trim(),
            name: name.Trim(),
            address: address);
    }

    public void UpdateDetails(
        string name,
        CountryCodes countryCode,
        string postalCode,
        string city,
        string street,
        PublicSpaces publicSpace,
        string houseNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var updatedAddress = Address.Create(
            countryCode: countryCode,
            postalCode: postalCode,
            city: city,
            street: street,
            publicSpace: publicSpace,
            houseNumber: houseNumber);

        Name = name.Trim();
        Address = updatedAddress;
    }
}