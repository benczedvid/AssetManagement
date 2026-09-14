using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Stores.UpdateStore
{
    public sealed record UpdateStoreRequest(
        string Name,
        AddressRequest Address
        );
}
