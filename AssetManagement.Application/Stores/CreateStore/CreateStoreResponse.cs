using AssetManagement.Application.Common.Models;


namespace AssetManagement.Application.Stores.CreateStore
{
    public sealed record CreateStoreResponse(
        Guid Id,
        string StoreNumber,
        string Name,
        AddressResponse Address
        );
}
