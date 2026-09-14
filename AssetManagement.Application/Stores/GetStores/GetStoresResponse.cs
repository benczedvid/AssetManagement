using AssetManagement.Application.Common.Models;


namespace AssetManagement.Application.Stores.GetStores;

public sealed record GetStoresResponse(
    Guid Id,
    string StoreNumber,
    string Name,
    AddressResponse Address
    );