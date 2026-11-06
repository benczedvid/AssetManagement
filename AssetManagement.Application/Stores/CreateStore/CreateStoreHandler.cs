using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.Stores;


namespace AssetManagement.Application.Stores.CreateStore
{
    public sealed class CreateStoreHandler
    {
        private readonly IStoreRepository _storeRepository;

        public CreateStoreHandler(IStoreRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);
            _storeRepository = repository;
        }

        public async Task<CreateStoreResponse> HandleAsync(CreateStoreRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var existingStore = await _storeRepository.GetByStoreNumberAsync(request.StoreNumber, cancellationToken);

            if (existingStore is not null) {
                throw new StoreNumberAlreadyExistsException(request.StoreNumber);
            }

            var newStore = Store.Create(
                storeNumber: request.StoreNumber,
                name: request.Name,
                countryCode: request.Address.CountryCode,
                postalCode: request.Address.PostalCode,
                city: request.Address.City,
                street: request.Address.Street,
                publicSpace: request.Address.PublicSpace,
                houseNumber: request.Address.HouseNumber);

            await _storeRepository.AddAsync(newStore, cancellationToken);
            await _storeRepository.SaveChangesAsync(cancellationToken);

            return new CreateStoreResponse(
                Id: newStore.Id,
                StoreNumber: newStore.StoreNumber,
                Name: newStore.Name,
                Address: new AddressResponse(
                    CountryCode: newStore.Address.CountryCode,
                    PostalCode: newStore.Address.PostalCode,
                    City: newStore.Address.City,
                    Street: newStore.Address.Street,
                    PublicSpace: newStore.Address.PublicSpace,
                    HouseNumber: newStore.Address.HouseNumber
                    ));
        }
    }
}
