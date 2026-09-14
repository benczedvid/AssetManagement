using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.ValueObjects.Address;

namespace AssetManagement.Application.Stores.GetStoreById
{
        public sealed record GetStoreByIdResponse(
            Guid Id,
            string StoreNumber,
            string Name,
            AddressResponse Address);
}
