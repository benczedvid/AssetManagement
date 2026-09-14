using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Stores;
using AssetManagement.Application.Stores.CreateStore;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Stores.CreateStore
{
    [TestClass]
    public sealed class CreateStoreHandlerTests
    {
        private FakeStoreRepository _storeRepository = null!;
        private CreateStoreHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _storeRepository = new FakeStoreRepository();

            _handler = new CreateStoreHandler(_storeRepository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new CreateStoreHandler(null!));

            Assert.AreEqual("repository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Request_Is_Null()
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                    () => _handler.HandleAsync(null!, CancellationToken.None));

            Assert.AreEqual("request", exception.ParamName);
            Assert.AreEqual(0, _storeRepository.GetByStoreNumberCallCount);
            Assert.AreEqual(0, _storeRepository.AddCallCount);
            Assert.AreEqual(0, _storeRepository.SaveChangesCallCount);
            Assert.IsEmpty(_storeRepository.Stores);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Create_And_Return_Store()
        {
            var request = CreateValidRequest();

            var response = await _handler.HandleAsync(request, CancellationToken.None);

            Assert.AreNotEqual(Guid.Empty, response.Id);
            Assert.AreEqual( request.StoreNumber, response.StoreNumber);
            Assert.AreEqual(request.Name, response.Name);
            Assert.IsNotNull(response.Address);
            Assert.AreEqual(request.Address.CountryCode, response.Address.CountryCode);
            Assert.AreEqual(request.Address.PostalCode, response.Address.PostalCode);
            Assert.AreEqual(request.Address.City, response.Address.City);
            Assert.AreEqual(request.Address.Street, response.Address.Street);
            Assert.AreEqual(request.Address.PublicSpace, response.Address.PublicSpace);
            Assert.AreEqual(request.Address.HouseNumber, response.Address.HouseNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Add_Store_To_Repository()
        {
            var request = CreateValidRequest();

            var response = await _handler.HandleAsync(request, CancellationToken.None);

            Assert.AreEqual(1, _storeRepository.AddCallCount);
            Assert.HasCount(1, _storeRepository.Stores);

            var persistedStore = _storeRepository.Stores.Single();

            Assert.AreEqual(response.Id, persistedStore.Id);
            Assert.AreEqual(request.StoreNumber, persistedStore.StoreNumber);
            Assert.AreEqual(request.Name, persistedStore.Name);
            Assert.AreEqual(request.Address.CountryCode, persistedStore.Address.CountryCode);
            Assert.AreEqual(request.Address.PostalCode, persistedStore.Address.PostalCode);
            Assert.AreEqual(request.Address.City, persistedStore.Address.City);
            Assert.AreEqual(request.Address.Street, persistedStore.Address.Street);
            Assert.AreEqual(request.Address.PublicSpace, persistedStore.Address.PublicSpace);
            Assert.AreEqual(request.Address.HouseNumber, persistedStore.Address.HouseNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Save_Changes()
        {
            var request = CreateValidRequest();

            await _handler.HandleAsync(request, CancellationToken.None);

            Assert.AreEqual(1, _storeRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken()
        {
            var request = CreateValidRequest();

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(request, cancellationToken);

            Assert.AreEqual(cancellationToken, _storeRepository.LastGetByStoreNumberCancellationToken);
            Assert.AreEqual(cancellationToken, _storeRepository.LastAddCancellationToken);
            Assert.AreEqual(cancellationToken, _storeRepository.LastSaveChangesCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_StoreNumber_Already_Exists()
        {
            var existingStore = new StoreBuilder()
                .WithStoreNumber("321")
                .Build();

            _storeRepository.Seed(existingStore);

            var request = CreateValidRequest(storeNumber: existingStore.StoreNumber);

            var exception = await Assert.ThrowsExactlyAsync<StoreNumberAlreadyExistsException>(
                    () => _handler.HandleAsync(
                        request,
                        CancellationToken.None));

            Assert.AreEqual(request.StoreNumber, exception.StoreNumber);
            Assert.AreEqual(1, _storeRepository.GetByStoreNumberCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Not_Add_Or_Save_When_StoreNumber_Already_Exists()
        {
            var existingStore = new StoreBuilder()
                .WithStoreNumber("321")
                .Build();

            _storeRepository.Seed(existingStore);

            var request = CreateValidRequest(storeNumber: existingStore.StoreNumber);

            await Assert.ThrowsExactlyAsync<StoreNumberAlreadyExistsException>(
                () => _handler.HandleAsync(
                    request,
                    CancellationToken.None));

            Assert.AreEqual(0, _storeRepository.AddCallCount);
            Assert.AreEqual(0, _storeRepository.SaveChangesCallCount);
            Assert.HasCount(1, _storeRepository.Stores);
            Assert.AreSame(existingStore, _storeRepository.Stores.Single());
        }

        [TestMethod]
        public async Task HandleAsync_Should_Trim_Store_Data()
        {
            var request = CreateValidRequest(
                storeNumber: "  321  ",
                name: "  Budapest M3  ",
                postalCode: "  1152  ",
                city: "  Budapest  ",
                street: "  Városkapu  ",
                houseNumber: "  5  ");

            var response = await _handler.HandleAsync(request, CancellationToken.None);

            Assert.AreEqual("321", response.StoreNumber);
            Assert.AreEqual("Budapest M3", response.Name);
            Assert.AreEqual("1152", response.Address.PostalCode);
            Assert.AreEqual("Budapest", response.Address.City);
            Assert.AreEqual("Városkapu", response.Address.Street);
            Assert.AreEqual("5", response.Address.HouseNumber);

            var persistedStore = _storeRepository.Stores.Single();

            Assert.AreEqual("321", persistedStore.StoreNumber);
            Assert.AreEqual("Budapest M3", persistedStore.Name);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Propagate_StoreNumber_Validation_Error()
        {
            var request = CreateValidRequest(storeNumber: " ");

            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                    () => _handler.HandleAsync(request, CancellationToken.None));

            Assert.AreEqual("storeNumber",exception.ParamName);
            Assert.AreEqual(1, _storeRepository.GetByStoreNumberCallCount);
            Assert.AreEqual(0, _storeRepository.AddCallCount);
            Assert.AreEqual(0, _storeRepository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Propagate_Name_Validation_Error()
        {
            var request = CreateValidRequest(name: " ");

            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                    () => _handler.HandleAsync(request, CancellationToken.None));

            Assert.AreEqual("name", exception.ParamName);
            Assert.AreEqual(0, _storeRepository.AddCallCount);
            Assert.AreEqual(0, _storeRepository.SaveChangesCallCount);
            Assert.IsEmpty(_storeRepository.Stores);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Propagate_Address_Validation_Error()
        {
            var request = CreateValidRequest(city: " ");

            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                    () => _handler.HandleAsync(request, CancellationToken.None));

            Assert.AreEqual("city", exception.ParamName);
            Assert.AreEqual(0, _storeRepository.AddCallCount);
            Assert.AreEqual(0, _storeRepository.SaveChangesCallCount);

            Assert.IsEmpty(_storeRepository.Stores);
        }

        private static CreateStoreRequest CreateValidRequest(
            string storeNumber = "321",
            string name = "Budapest M3",
            CountryCodes countryCode = CountryCodes.HUN,
            string postalCode = "1152",
            string city = "Budapest",
            string street = "Városkapu",
            PublicSpaces publicSpace = PublicSpaces.Street,
            string houseNumber = "5")
        {
            return new CreateStoreRequest(
                StoreNumber: storeNumber,
                Name: name,
                Address: new AddressRequest(
                    CountryCode: countryCode,
                    PostalCode: postalCode,
                    City: city,
                    Street: street,
                    PublicSpace: publicSpace,
                    HouseNumber: houseNumber));
        }
    }
}