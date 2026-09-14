using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Stores.UpdateStore
{
    public sealed record UpdateStoreResponse(
        Guid Id,
        string StoreNumber,
        string Name,
        AddressResponse Address
        );
}
