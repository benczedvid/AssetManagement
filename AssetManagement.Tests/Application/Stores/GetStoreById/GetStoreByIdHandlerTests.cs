using AssetManagement.Application.Stores;
using AssetManagement.Application.Stores.GetStoreById;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Stores.GetStoreById
{
    [TestClass]
    public sealed class GetStoreByIdHandlerTests
    {
        private FakeStoreRepository _storeRepository = null!;
        private GetStoreByIdHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _storeRepository = new FakeStoreRepository();
            _handler = new GetStoreByIdHandler(_storeRepository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new GetStoreByIdHandler(null!));

            Assert.AreEqual("repository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Id_Is_Empty()
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                    () => _handler.HandleAsync(
                        Guid.Empty,
                        CancellationToken.None));

            Assert.AreEqual("id", exception.ParamName);
            Assert.AreEqual(0, _storeRepository.GetByIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Store_When_Store_Exists()
        {
            var store = new StoreBuilder()
                .WithStoreNumber("321")
                .WithName("Budapest M3")
                .WithCountryCode(CountryCodes.HUN)
                .WithPostalCode("1152")
                .WithCity("Budapest")
                .WithStreet("Városkapu")
                .WithPublicSpace(PublicSpaces.Street)
                .WithHouseNumber("5")
                .Build();

            _storeRepository.Seed(store);

            var response = await _handler.HandleAsync(store.Id, CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(store.Id, response.Id);
            Assert.AreEqual(store.StoreNumber, response.StoreNumber);
            Assert.AreEqual(store.Name, response.Name);
            Assert.IsNotNull(response.Address);
            Assert.AreEqual(store.Address.CountryCode, response.Address.CountryCode);
            Assert.AreEqual(store.Address.PostalCode, response.Address.PostalCode);
            Assert.AreEqual(store.Address.City, response.Address.City);
            Assert.AreEqual(store.Address.Street, response.Address.Street);
            Assert.AreEqual(store.Address.PublicSpace, response.Address.PublicSpace);
            Assert.AreEqual( store.Address.HouseNumber, response.Address.HouseNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Query_Repository_With_Requested_Id()
        {
            var store = new StoreBuilder().Build();

            _storeRepository.Seed(store);

            await _handler.HandleAsync(store.Id, CancellationToken.None);

            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Does_Not_Exist()
        {
            var unknownId = Guid.NewGuid();

            var exception = await Assert.ThrowsExactlyAsync<StoreNotFoundException>(
                    () => _handler.HandleAsync(unknownId, CancellationToken.None));

            Assert.AreEqual(unknownId, exception.Id);
            Assert.AreEqual(1, _storeRepository.GetByIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken()
        {
            var store = new StoreBuilder().Build();

            _storeRepository.Seed(store);

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(store.Id, cancellationToken);

            Assert.AreEqual(cancellationToken, _storeRepository.LastGetByIdCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_When_Store_Does_Not_Exist()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await Assert.ThrowsExactlyAsync<StoreNotFoundException>(
                () => _handler.HandleAsync(
                    Guid.NewGuid(),
                    cancellationToken));

            Assert.AreEqual(cancellationToken, _storeRepository.LastGetByIdCancellationToken);
        }
    }
}