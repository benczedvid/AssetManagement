using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Stores.UpdateStore
{
    /// <summary>
    /// Represents the result of successfully updating an existing store.
    /// </summary>
    /// <param name="Id">The unique identifier of the updated store.</param>
    /// <param name="StoreNumber">The unique store number used to identify the store.</param>
    /// <param name="Name">The current name of the store.</param>
    /// <param name="Address">The current address of the store.</param>
    public sealed record UpdateStoreResponse(
        Guid Id,
        string StoreNumber,
        string Name,
        AddressResponse Address
        );
}
