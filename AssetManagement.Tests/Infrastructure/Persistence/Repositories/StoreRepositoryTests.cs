using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Infrastructure.Persistence.Repositories;
using AssetManagement.Tests.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;


namespace AssetManagement.Tests.Infrastructure.Persistence.Repositories
{
    [TestClass]
    public class StoreRepositoryTests
    {
        private SqliteConnection _sqliteConnection = null!;
        private AppDbContext _appDbContext = null!;
        private StoreRepository _storeRepository = null!;

        [TestInitialize]
        public void Initialize()
        {
            _sqliteConnection = new SqliteConnection("Data Source=:memory:");
            _sqliteConnection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_sqliteConnection).Options;

            _appDbContext = new AppDbContext(options);
            _appDbContext.Database.EnsureCreated();
            _storeRepository = new StoreRepository(_appDbContext);
        }
        [TestCleanup]
        public void Cleanup()
        {
            _appDbContext?.Dispose();
            _sqliteConnection?.Dispose();
        }

        [TestMethod]
        public async Task AddAsync_And_SaveChangesAsync_Should_Persist_Store()
        {
            var store = new StoreBuilder().Build();

            await _storeRepository!.AddAsync(store, CancellationToken.None);
            await _storeRepository.SaveChangesAsync(CancellationToken.None);

            var persistedStore = await _appDbContext!.Stores.SingleOrDefaultAsync(candidate => candidate.Id == store.Id);

            Assert.IsNotNull(persistedStore);

            Assert.AreEqual(store.Id, persistedStore.Id);
            Assert.AreEqual(store.StoreNumber, persistedStore.StoreNumber);
            Assert.AreEqual(store.Name, persistedStore.Name);
        }
        [TestMethod]
        public async Task GetByIdAsync_Should_Return_Store_When_Store_Exists()
        {
            var existingStore = new StoreBuilder().Build();

            _appDbContext.Stores.Add(existingStore);
            await _appDbContext.SaveChangesAsync();

            var result = await _storeRepository.GetByIdAsync(existingStore.Id);

            Assert.IsNotNull(result);

            Assert.AreEqual(existingStore.Id, result.Id);
            Assert.AreEqual(existingStore.StoreNumber, result.StoreNumber);
            Assert.AreEqual(existingStore.Name, result.Name);
        }
        [TestMethod]
        public async Task GetByIdAsync_Should_Return_Null_When_Store_Does_Not_Exist()
        {
            var unknownStoreId = Guid.NewGuid();

            var result = await _storeRepository.GetByIdAsync(unknownStoreId);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetByStoreNumberAsync_Should_Return_Store_When_Store_Exists()
        {
            var store = new StoreBuilder().Build();

            _appDbContext.Stores.Add(store);
            await _appDbContext.SaveChangesAsync();

            var result = await _storeRepository.GetByStoreNumberAsync(store.StoreNumber);

            Assert.IsNotNull(result);

            Assert.AreEqual(store.Id, result.Id);
            Assert.AreEqual(store.StoreNumber, result.StoreNumber);
            Assert.AreEqual(store.Name, result.Name);
        }

        [TestMethod]
        public async Task GetByStoreNumberAsync_Should_Return_Null_When_Store_Does_Not_Exist()
        {
            string unknownStoreNumber = "000";

            var result = await _storeRepository.GetByStoreNumberAsync(unknownStoreNumber);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SaveChangesAsync_Should_Throw_When_Store_Number_Is_Duplicated()
        {
            string existingStoreNumber = "999";
            string newStoreNumber = "999";

            var existingStore = new StoreBuilder().WithStoreNumber(existingStoreNumber).Build();
            _appDbContext.Stores.Add(existingStore);
            await _appDbContext.SaveChangesAsync();

            var newStore = new StoreBuilder().WithStoreNumber(newStoreNumber).Build();
            _appDbContext.Stores.Add(newStore);

            await Assert.ThrowsExactlyAsync<DbUpdateException>(() => _appDbContext.SaveChangesAsync());
        }
        [TestMethod]
        public async Task GetAllAsync_Should_Return_All_Stores()
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

            _appDbContext.Add(firstStore);
            await _appDbContext.SaveChangesAsync();

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
            _appDbContext.Add(secondStore);
            await _appDbContext.SaveChangesAsync();

            var result = await _storeRepository.GetAllAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);

            var firstStoreResult = result.Single(store => store.Id == firstStore.Id);
            Assert.AreEqual(firstStore.Id, firstStoreResult.Id);
            Assert.AreEqual(firstStore.StoreNumber, firstStoreResult.StoreNumber);
            Assert.AreEqual(firstStore.Name, firstStoreResult.Name);
            Assert.AreEqual(firstStore.Address.CountryCode, firstStoreResult.Address.CountryCode);
            Assert.AreEqual(firstStore.Address.PostalCode, firstStoreResult.Address.PostalCode);
            Assert.AreEqual(firstStore.Address.City, firstStoreResult.Address.City);
            Assert.AreEqual(firstStore.Address.Street, firstStoreResult.Address.Street);
            Assert.AreEqual(firstStore.Address.PublicSpace, firstStoreResult.Address.PublicSpace);
            Assert.AreEqual(firstStore.Address.HouseNumber, firstStoreResult.Address.HouseNumber);
        }

        [TestMethod]
        public async Task GetAllAsync_Should_Return_Empty_List_When_No_Stores_Exist()
        {
            var result = await _storeRepository.GetAllAsync();
            Assert.IsEmpty(result);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_DbContext_Is_Null()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new StoreRepository(null!));
        }

        [TestMethod]
        public async Task AddAsync_Should_Throw_When_Store_Is_Null()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _storeRepository.AddAsync(null!));
        }
        [TestMethod]
        public async Task GetForUpdateByIdAsync_Should_Return_Tracked_Store_When_Store_Exists()
        {
            var store = new StoreBuilder().Build();

            _appDbContext.Stores.Add(store);
            await _appDbContext.SaveChangesAsync();
            _appDbContext.ChangeTracker.Clear();

            var result = await _storeRepository.GetForUpdateByIdAsync(store.Id, CancellationToken.None);

            Assert.IsNotNull(result);

            var entry = _appDbContext.Entry(result);

            Assert.AreEqual(EntityState.Unchanged, entry.State);
        }

        [TestMethod]
        public async Task GetForUpdateByIdAsync_Should_Track_Changes_When_Store_Is_Modified()
        {
            var store = new StoreBuilder().Build();

            _appDbContext.Stores.Add(store);
            await _appDbContext.SaveChangesAsync();
            _appDbContext.ChangeTracker.Clear();

            var trackedStore = await _storeRepository.GetForUpdateByIdAsync(store.Id);
            Assert.IsNotNull(trackedStore);

            trackedStore.UpdateDetails(
                name: "Pécs",
                countryCode: CountryCodes.HUN,
                postalCode: "7634",
                city: "Pécs",
                street: "Makay István",
                publicSpace: PublicSpaces.Street,
                houseNumber: "11");
            _appDbContext.ChangeTracker.DetectChanges();

            Assert.AreEqual(EntityState.Modified, _appDbContext.Entry(trackedStore).State);
        }

        [TestMethod]
        public async Task GetForUpdateByIdAsync_Should_Return_Null_When_Store_Does_Not_Exist()
        {
            Guid unknownStore = Guid.NewGuid();

            var result = await _storeRepository.GetForUpdateByIdAsync(unknownStore, CancellationToken.None);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetForUpdateByIdAsync_And_SaveChangesAsync_Should_Persist_Updated_Values()
        {
            var store = new StoreBuilder().Build();

            _appDbContext.Stores.Add(store);
            await _appDbContext.SaveChangesAsync();
            _appDbContext.ChangeTracker.Clear();

            var trackedStore = await _storeRepository.GetForUpdateByIdAsync(store.Id, CancellationToken.None);
            Assert.IsNotNull(trackedStore);

            trackedStore.UpdateDetails(
                name: "Pécs",
                countryCode: CountryCodes.HUN,
                postalCode: "7634",
                city: "Pécs",
                street: "Makay István",
                publicSpace: PublicSpaces.Street,
                houseNumber: "11");
            await _storeRepository.SaveChangesAsync();
            _appDbContext.ChangeTracker.Clear();

            var persistedStore = await _storeRepository.GetByIdAsync(trackedStore.Id, CancellationToken.None);
            Assert.IsNotNull(persistedStore);

            Assert.AreEqual(store.Id, persistedStore.Id);
            Assert.AreEqual(store.StoreNumber, persistedStore.StoreNumber);
            Assert.AreEqual("Pécs", persistedStore.Name);
            Assert.AreEqual(CountryCodes.HUN, persistedStore.Address.CountryCode);
            Assert.AreEqual("7634", persistedStore.Address.PostalCode);
            Assert.AreEqual("Pécs", persistedStore.Address.City);
            Assert.AreEqual("Makay István", persistedStore.Address.Street);
            Assert.AreEqual(PublicSpaces.Street, persistedStore.Address.PublicSpace);
            Assert.AreEqual("11", persistedStore.Address.HouseNumber);
        }
    }
}
