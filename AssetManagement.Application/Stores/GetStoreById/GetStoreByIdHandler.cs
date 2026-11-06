using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.Stores;

namespace AssetManagement.Application.Stores.GetStoreById
{
    public sealed class GetStoreByIdHandler
    {
        private readonly IStoreRepository _repository;

        public GetStoreByIdHandler(IStoreRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);

            _repository = repository;
        }

        public async Task<GetStoreByIdResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("The store identifier cannot be empty.", nameof(id));
            }

            var store = await _repository.GetByIdAsync(id, cancellationToken);

            if (store is null)
            {
                throw new StoreNotFoundException(id);
            }

            return Map(store);
        }

        private static GetStoreByIdResponse Map(Store store)
        {
            return new GetStoreByIdResponse(
                Id: store.Id,
                StoreNumber: store.StoreNumber,
                Name: store.Name,
                Address: new AddressResponse(
                    CountryCode: store.Address.CountryCode,
                    PostalCode: store.Address.PostalCode,
                    City: store.Address.City,
                    Street: store.Address.Street,
                    PublicSpace: store.Address.PublicSpace,
                    HouseNumber: store.Address.HouseNumber));
        }
    }
}