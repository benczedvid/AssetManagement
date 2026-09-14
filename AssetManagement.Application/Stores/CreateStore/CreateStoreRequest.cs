using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Stores.CreateStore
{
    public sealed record CreateStoreRequest(
        string StoreNumber,
        string Name,
        AddressRequest Address
        );
}
