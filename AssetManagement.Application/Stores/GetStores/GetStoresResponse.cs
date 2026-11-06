using AssetManagement.Application.Common.Models;


namespace AssetManagement.Application.Stores.GetStores;

/// <summary>
/// Represents summary information about a store returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the store.</param>
/// <param name="StoreNumber">The unique store number used to identify the store.</param>
/// <param name="Name">The name of the store.</param>
/// <param name="Address">The complete address of the store.</param>
public sealed record GetStoresResponse(
    Guid Id,
    string StoreNumber,
    string Name,
    AddressResponse Address
    );