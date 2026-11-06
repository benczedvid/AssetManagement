using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Stores.UpdateStore
{
    /// <summary>
    /// Represents a request to update an existing store.
    /// </summary>
    /// <param name="Name">
    /// The updated name of the store.
    /// </param>
    /// <param name="Address">
    /// The updated address of the store.
    /// </param>
    public sealed record UpdateStoreRequest(
        string Name,
        AddressRequest Address
        );
}
