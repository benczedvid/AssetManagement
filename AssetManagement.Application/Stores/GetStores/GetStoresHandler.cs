using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.ValueObjects.Address;

namespace AssetManagement.Application.Stores.GetStores
{
    public sealed class GetStoresHandler
    {
        private readonly IStoreRepository _storeRepository;

        public GetStoresHandler(IStoreRepository storeRepository)
        {
            ArgumentNullException.ThrowIfNull(storeRepository);
            _storeRepository = storeRepository;
        }

        public async Task<IReadOnlyList<GetStoresResponse>> HandleAsync(CancellationToken cancellationToken = default)
        {
            var stores = await _storeRepository.GetAllAsync(cancellationToken);

            return [.. stores.Select(Map)];
        }
        private static GetStoresResponse Map(Store store)
        {
        return new GetStoresResponse(
            Id: store.Id,
            StoreNumber: store.StoreNumber,
            Name: store.Name,
            Address: new AddressResponse(
                CountryCode:store.Address.CountryCode,
                PostalCode: store.Address.PostalCode,
                City: store.Address.City,
                Street: store.Address.Street,
                PublicSpace: store.Address.PublicSpace,
                HouseNumber: store.Address.HouseNumber
                )
            );
        }
    }
}
