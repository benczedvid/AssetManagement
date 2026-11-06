using AssetManagement.Application.Common.Models;


namespace AssetManagement.Application.Stores.CreateStore
{
    /// <summary>
    /// Represents the result of successfully creating a new store.
    /// </summary>
    /// <param name="Id">The unique identifier of the created store.</param>
    /// <param name="StoreNumber">The unique store number used to identify the store.</param>
    /// <param name="Name">The name of the store.</param>
    /// <param name="Address">The complete address of the store.</param>
    public sealed record CreateStoreResponse(
        Guid Id,
        string StoreNumber,
        string Name,
        AddressResponse Address
        );
}
