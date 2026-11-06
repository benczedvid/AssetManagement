using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Stores.CreateStore
{
    /// <summary>
    /// Represents a request to create a new store.
    /// </summary>
    /// <param name="StoreNumber">
    /// The unique store number used to identify the store.
    /// </param>
    /// <param name="Name">
    /// The name of the store.
    /// </param>
    /// <param name="Address">
    /// The address of the store.
    /// </param>
    public sealed record CreateStoreRequest(
        string StoreNumber,
        string Name,
        AddressRequest Address
        );
}
