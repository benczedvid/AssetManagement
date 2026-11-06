using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.Stores;

namespace AssetManagement.Application.Stores.UpdateStore
{
    public sealed class UpdateStoreHandler
    {
        private readonly IStoreRepository _repository;

        public UpdateStoreHandler(IStoreRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);

            _repository = repository;
        }

        public async Task<UpdateStoreResponse> HandleAsync(
            Guid id,
            UpdateStoreRequest request,
            CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("The store identifier cannot be empty.", nameof(id));
            }

            ArgumentNullException.ThrowIfNull(request);

            var existingStore = await _repository.GetForUpdateByIdAsync(
                    id,
                    cancellationToken)
                ?? throw new StoreNotFoundException(id);

            existingStore.UpdateDetails(
                name: request.Name,
                countryCode: request.Address.CountryCode,
                postalCode: request.Address.PostalCode,
                city: request.Address.City,
                street: request.Address.Street,
                publicSpace: request.Address.PublicSpace,
                houseNumber: request.Address.HouseNumber);

            await _repository.SaveChangesAsync(cancellationToken);

            return Map(existingStore);
        }

        private static UpdateStoreResponse Map(Store store)
        {
            return new UpdateStoreResponse(
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