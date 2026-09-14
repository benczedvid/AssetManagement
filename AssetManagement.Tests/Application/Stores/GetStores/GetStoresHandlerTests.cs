using AssetManagement.Application.Stores.GetStores;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Stores.GetStores
{
    [TestClass]
    public sealed class GetStoresHandlerTests
    {
        private FakeStoreRepository _repository = null!;
        private GetStoresHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _repository = new FakeStoreRepository();
            _handler = new GetStoresHandler(_repository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new GetStoresHandler(null!));

            Assert.AreEqual("storeRepository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_All_Stores()
        {
            var firstStore = new StoreBuilder()
                .WithStoreNumber("321")
                .WithName("Budapest M3")
                .WithCountryCode(CountryCodes.HUN)
                .WithPostalCode("1152")
                .WithCity("Budapest")
                .WithStreet("Városkapu")
                .WithPublicSpace(PublicSpaces.Street)
                .WithHouseNumber("5")
                .Build();

            var secondStore = new StoreBuilder()
                .WithStoreNumber("322")
                .WithName("Pécs")
                .WithCountryCode(CountryCodes.HUN)
                .WithPostalCode("7634")
                .WithCity("Pécs")
                .WithStreet("Makay István")
                .WithPublicSpace(PublicSpaces.Street)
                .WithHouseNumber("11")
                .Build();

            _repository.Seed(firstStore);
            _repository.Seed(secondStore);

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.HasCount(2, response);
            Assert.IsTrue(response.Any(store => store.Id == firstStore.Id));
            Assert.IsTrue(response.Any(store => store.Id == secondStore.Id));
        }

        [TestMethod]
        public async Task HandleAsync_Should_Map_First_Store()
        {
            var firstStore = new StoreBuilder()
                .WithStoreNumber("321")
                .WithName("Budapest M3")
                .WithCountryCode(CountryCodes.HUN)
                .WithPostalCode("1152")
                .WithCity("Budapest")
                .WithStreet("Városkapu")
                .WithPublicSpace(PublicSpaces.Street)
                .WithHouseNumber("5")
                .Build();

            var secondStore = new StoreBuilder()
                .WithStoreNumber("322")
                .WithName("Pécs")
                .WithPostalCode("7634")
                .WithCity("Pécs")
                .WithStreet("Makay István")
                .WithPublicSpace(PublicSpaces.Street)
                .WithHouseNumber("11")
                .Build();

            _repository.Seed(firstStore);
            _repository.Seed(secondStore);

            var response = await _handler.HandleAsync(CancellationToken.None);
            var responseStore = response.Single(store => store.Id == firstStore.Id);

            Assert.AreEqual(firstStore.Id, responseStore.Id);
            Assert.AreEqual(firstStore.StoreNumber, responseStore.StoreNumber);
            Assert.AreEqual(firstStore.Name, responseStore.Name);
            Assert.IsNotNull(responseStore.Address);
            Assert.AreEqual(firstStore.Address.CountryCode, responseStore.Address.CountryCode);
            Assert.AreEqual(firstStore.Address.PostalCode, responseStore.Address.PostalCode);
            Assert.AreEqual(firstStore.Address.City, responseStore.Address.City);
            Assert.AreEqual(firstStore.Address.Street, responseStore.Address.Street);
            Assert.AreEqual(firstStore.Address.PublicSpace, responseStore.Address.PublicSpace);
            Assert.AreEqual(firstStore.Address.HouseNumber, responseStore.Address.HouseNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Map_Second_Store()
        {
            var firstStore = new StoreBuilder()
                .WithStoreNumber("321")
                .WithName("Budapest M3")
                .Build();

            var secondStore = new StoreBuilder()
                .WithStoreNumber("322")
                .WithName("Pécs")
                .WithCountryCode(CountryCodes.HUN)
                .WithPostalCode("7634")
                .WithCity("Pécs")
                .WithStreet("Makay István")
                .WithPublicSpace(PublicSpaces.Street)
                .WithHouseNumber("11")
                .Build();

            _repository.Seed(firstStore);
            _repository.Seed(secondStore);

            var response = await _handler.HandleAsync(CancellationToken.None);

            var responseStore = response.Single(store => store.Id == secondStore.Id);

            Assert.AreEqual(secondStore.Id, responseStore.Id);
            Assert.AreEqual(secondStore.StoreNumber, responseStore.StoreNumber);
            Assert.AreEqual(secondStore.Name, responseStore.Name);
            Assert.IsNotNull(responseStore.Address);
            Assert.AreEqual(secondStore.Address.CountryCode, responseStore.Address.CountryCode);
            Assert.AreEqual(secondStore.Address.PostalCode, responseStore.Address.PostalCode);
            Assert.AreEqual(secondStore.Address.City, responseStore.Address.City);
            Assert.AreEqual(secondStore.Address.Street, responseStore.Address.Street);
            Assert.AreEqual(secondStore.Address.PublicSpace, responseStore.Address.PublicSpace);
            Assert.AreEqual(secondStore.Address.HouseNumber, responseStore.Address.HouseNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Preserve_Repository_Order()
        {
            var firstStore = new StoreBuilder()
                .WithStoreNumber("321")
                .Build();

            var secondStore = new StoreBuilder()
                .WithStoreNumber("322")
                .Build();

            _repository.Seed(firstStore);
            _repository.Seed(secondStore);

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.HasCount(2, response);
            Assert.AreEqual(firstStore.Id, response[0].Id);
            Assert.AreEqual(secondStore.Id, response[1].Id);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Empty_List_When_No_Store_Exists()
        {
            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.IsEmpty(response);

            Assert.AreEqual(1, _repository.GetAllCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Call_Repository_Once()
        {
            _repository.Seed(new StoreBuilder().Build());

            await _handler.HandleAsync(CancellationToken.None);

            Assert.AreEqual(1, _repository.GetAllCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken()
        {
            _repository.Seed(new StoreBuilder().Build());

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(cancellationToken);

            Assert.AreEqual(cancellationToken, _repository.LastGetAllCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_When_No_Store_Exists()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            var response = await _handler.HandleAsync(cancellationToken);

            Assert.IsEmpty(response);
            Assert.AreEqual(cancellationToken, _repository.LastGetAllCancellationToken);
        }
    }
}