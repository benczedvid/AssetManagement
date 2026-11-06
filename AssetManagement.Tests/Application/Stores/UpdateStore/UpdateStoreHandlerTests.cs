using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Stores;
using AssetManagement.Application.Stores.UpdateStore;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Stores.UpdateStore
{
    [TestClass]
    public sealed class UpdateStoreHandlerTests
    {
        private FakeStoreRepository _repository = null!;
        private UpdateStoreHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _repository = new FakeStoreRepository();
            _handler = new UpdateStoreHandler(_repository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new UpdateStoreHandler(null!));

            Assert.AreEqual("repository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Id_Is_Empty()
        {
            var request = CreateValidRequest();

            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                    () => _handler.HandleAsync(
                        Guid.Empty,
                        request,
                        CancellationToken.None));

            Assert.AreEqual("id", exception.ParamName);
            Assert.AreEqual(0, _repository.GetForUpdateByIdCallCount);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Request_Is_Null()
        {
            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                    () => _handler.HandleAsync(
                        Guid.NewGuid(),
                        null!,
                        CancellationToken.None));

            Assert.AreEqual("request", exception.ParamName);
            Assert.AreEqual(0, _repository.GetForUpdateByIdCallCount);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Update_Store()
        {
            var store = new StoreBuilder()
                .WithStoreNumber("321")
                .WithName("Budapest M3")
                .Build();

            _repository.Seed(store);

            var request = CreateValidRequest();

            await _handler.HandleAsync(store.Id, request, CancellationToken.None);

            Assert.AreEqual(request.Name, store.Name);
            Assert.AreEqual(request.Address.CountryCode, store.Address.CountryCode);
            Assert.AreEqual(request.Address.PostalCode, store.Address.PostalCode);
            Assert.AreEqual(request.Address.City, store.Address.City);
            Assert.AreEqual(request.Address.Street, store.Address.Street);
            Assert.AreEqual(request.Address.PublicSpace, store.Address.PublicSpace);
            Assert.AreEqual(request.Address.HouseNumber, store.Address.HouseNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Updated_Store()
        {
            var store = new StoreBuilder()
                .WithStoreNumber("321")
                .WithName("Budapest M3")
                .Build();

            _repository.Seed(store);

            var request = CreateValidRequest();

            var response = await _handler.HandleAsync(store.Id, request, CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(store.Id, response.Id);
            Assert.AreEqual(store.StoreNumber, response.StoreNumber);
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
        public async Task HandleAsync_Should_Preserve_Id_And_StoreNumber()
        {
            var store = new StoreBuilder()
                .WithStoreNumber("321")
                .WithName("Budapest M3")
                .Build();

            var originalId = store.Id;
            var originalStoreNumber = store.StoreNumber;

            _repository.Seed(store);

            var request = CreateValidRequest();

            var response = await _handler.HandleAsync(store.Id, request, CancellationToken.None);

            Assert.AreEqual(originalId, store.Id);
            Assert.AreEqual(originalStoreNumber, store.StoreNumber);
            Assert.AreEqual(originalId, response.Id);
            Assert.AreEqual(originalStoreNumber, response.StoreNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Trim_Updated_Values()
        {
            var store = new StoreBuilder().Build();

            _repository.Seed(store);

            var request = CreateValidRequest(
                name: "  Pécs  ",
                postalCode: "  7634  ",
                city: "  Pécs  ",
                street: "  Makay István  ",
                houseNumber: "  11  ");

            var response = await _handler.HandleAsync(
                store.Id,
                request,
                CancellationToken.None);

            Assert.AreEqual("Pécs", store.Name);
            Assert.AreEqual("7634", store.Address.PostalCode);
            Assert.AreEqual("Pécs", store.Address.City);
            Assert.AreEqual("Makay István", store.Address.Street);
            Assert.AreEqual("11", store.Address.HouseNumber);
            Assert.AreEqual("Pécs", response.Name);
            Assert.AreEqual("7634", response.Address.PostalCode);
            Assert.AreEqual("Pécs", response.Address.City);
            Assert.AreEqual("Makay István", response.Address.Street);
            Assert.AreEqual("11", response.Address.HouseNumber);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Replace_Address_Instance()
        {
            var store = new StoreBuilder().Build();
            var originalAddress = store.Address;

            _repository.Seed(store);

            await _handler.HandleAsync(store.Id, CreateValidRequest(), CancellationToken.None);

            Assert.AreNotSame(originalAddress, store.Address);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Save_Changes()
        {
            var store = new StoreBuilder().Build();

            _repository.Seed(store);

            await _handler.HandleAsync(store.Id, CreateValidRequest(), CancellationToken.None);

            Assert.AreEqual(1, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Query_Store_For_Update()
        {
            var store = new StoreBuilder().Build();

            _repository.Seed(store);

            await _handler.HandleAsync(store.Id, CreateValidRequest(), CancellationToken.None);

            Assert.AreEqual(1, _repository.GetForUpdateByIdCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Store_Does_Not_Exist()
        {
            var unknownStoreId = Guid.NewGuid();
            var request = CreateValidRequest();

            var exception = await Assert.ThrowsExactlyAsync<StoreNotFoundException>(
                    () => _handler.HandleAsync(
                        unknownStoreId,
                        request,
                        CancellationToken.None));

            Assert.AreEqual(unknownStoreId, exception.Id);
            Assert.AreEqual(1, _repository.GetForUpdateByIdCallCount);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
            Assert.IsEmpty(_repository.Stores);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Not_Save_When_Domain_Validation_Fails()
        {
            var store = new StoreBuilder().Build();

            var originalName = store.Name;
            var originalAddress = store.Address;

            _repository.Seed(store);

            var request = CreateValidRequest(city: " ");

            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                    () => _handler.HandleAsync(store.Id, request, CancellationToken.None));

            Assert.AreEqual("city", exception.ParamName);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
            Assert.AreEqual(originalName, store.Name);
            Assert.AreSame(originalAddress, store.Address);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_Name_Is_Null()
        {
            var store = new StoreBuilder().Build();

            _repository.Seed(store);

            var request = CreateValidRequest(
                name: null!);

            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                    () => _handler.HandleAsync(
                        store.Id,
                        request,
                        CancellationToken.None));

            Assert.AreEqual("name", exception.ParamName);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public async Task HandleAsync_Should_Throw_When_Name_Is_Empty_Or_Whitespace(
            string invalidName)
        {
            var store = new StoreBuilder().Build();

            _repository.Seed(store);

            var request = CreateValidRequest(name: invalidName);

            var exception = await Assert.ThrowsExactlyAsync< ArgumentException>(
                    () => _handler.HandleAsync(
                        store.Id,
                        request,
                        CancellationToken.None));

            Assert.AreEqual("name", exception.ParamName);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_CountryCode_Is_Unknown()
        {
            var store = new StoreBuilder().Build();

            _repository.Seed(store);

            var request = CreateValidRequest(countryCode: CountryCodes.Unknown);

            var exception = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
                    () => _handler.HandleAsync(
                        store.Id,
                        request,
                        CancellationToken.None));

            Assert.AreEqual("countryCode", exception.ParamName);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Throw_When_PublicSpace_Is_Unknown()
        {
            var store = new StoreBuilder().Build();

            _repository.Seed(store);

            var request = CreateValidRequest(publicSpace: PublicSpaces.Unknown);

            var exception = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
                    () => _handler.HandleAsync(
                        store.Id,
                        request,
                        CancellationToken.None));

            Assert.AreEqual("publicSpace", exception.ParamName);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken()
        {
            var store = new StoreBuilder().Build();

            _repository.Seed(store);

            var request = CreateValidRequest();

            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(store.Id, request, cancellationToken);

            Assert.AreEqual(cancellationToken, _repository.LastGetForUpdateByIdCancellationToken);
            Assert.AreEqual(cancellationToken, _repository.LastSaveChangesCancellationToken);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Forward_CancellationToken_When_Store_Does_Not_Exist()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await Assert.ThrowsExactlyAsync<StoreNotFoundException>(
                () => _handler.HandleAsync(
                    Guid.NewGuid(),
                    CreateValidRequest(),
                    cancellationToken));

            Assert.AreEqual(cancellationToken, _repository.LastGetForUpdateByIdCancellationToken);
            Assert.AreEqual(0, _repository.SaveChangesCallCount);
        }

        private static UpdateStoreRequest CreateValidRequest(
            string name = "Pécs",
            CountryCodes countryCode = CountryCodes.HUN,
            string postalCode = "7634",
            string city = "Pécs",
            string street = "Makay István",
            PublicSpaces publicSpace = PublicSpaces.Street,
            string houseNumber = "11")
        {
            return new UpdateStoreRequest(
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