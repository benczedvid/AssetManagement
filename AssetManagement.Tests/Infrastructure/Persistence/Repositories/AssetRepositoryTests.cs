using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Infrastructure.Persistence.Repositories;
using AssetManagement.Tests.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Tests.Infrastructure.Persistence.Repositories;

[TestClass]
public sealed class AssetRepositoryTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _dbContext = null!;
    private AssetRepository _repository = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);

        await _dbContext.Database.EnsureCreatedAsync();

        _repository = new AssetRepository(_dbContext);
    }

    [TestCleanup]
    public async Task CleanupAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [TestMethod]
    public void Constructor_Should_Throw_When_DbContext_Is_Null()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new AssetRepository(null!));
    }

    [TestMethod]
    public async Task AddAsync_Should_Add_Asset_To_Context()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var asset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "ADD-001");

        // Act
        await _repository.AddAsync(
            asset,
            CancellationToken.None);

        // Assert
        var entry = _dbContext.Entry(asset);

        Assert.AreEqual(EntityState.Added, entry.State);
    }

    [TestMethod]
    public async Task SaveChangesAsync_Should_Persist_Added_Asset()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var asset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "SAVE-001");

        await _repository.AddAsync(
            asset,
            CancellationToken.None);

        // Act
        await _repository.SaveChangesAsync(
            CancellationToken.None);

        _dbContext.ChangeTracker.Clear();

        // Assert
        var persistedAsset = await _dbContext.Assets
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == asset.Id);

        Assert.IsNotNull(persistedAsset);
        Assert.AreEqual(asset.Id, persistedAsset.Id);
        Assert.AreEqual(asset.AssetName, persistedAsset.AssetName);
        Assert.AreEqual(asset.SerialNumber, persistedAsset.SerialNumber);
        Assert.AreEqual(
            testData.Store.Id,
            persistedAsset.AssignedStoreId);

        Assert.AreEqual(
            testData.User.Id,
            persistedAsset.AssignedUserId);
    }

    [TestMethod]
    public async Task GetAllAsync_Should_Return_All_Assets()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var firstAsset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "ALL-001");

        var secondAsset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "ALL-002");

        await SeedAssetsAsync(firstAsset, secondAsset);

        // Act
        var result = await _repository.GetAllAsync(
            CancellationToken.None);

        // Assert
        Assert.HasCount(2, result);

        Assert.IsTrue(result.Any(asset =>
            asset.Id == firstAsset.Id));

        Assert.IsTrue(result.Any(asset =>
            asset.Id == secondAsset.Id));
    }

    [TestMethod]
    public async Task GetAllAsync_Should_Return_Empty_List_When_No_Assets_Exist()
    {
        // Act
        var result = await _repository.GetAllAsync(
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task GetAllAsync_Should_Return_Untracked_Assets()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var asset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "ALL-TRACKING-001");

        await SeedAssetsAsync(asset);

        // Act
        var result = await _repository.GetAllAsync(
            CancellationToken.None);

        var returnedAsset = result.Single();

        // Assert
        Assert.AreEqual(
            EntityState.Detached,
            _dbContext.Entry(returnedAsset).State);
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Asset_When_Asset_Exists()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var asset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "ID-001");

        await SeedAssetsAsync(asset);

        // Act
        var result = await _repository.GetByIdAsync(
            asset.Id,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(asset.Id, result.Id);
        Assert.AreEqual(asset.AssetName, result.AssetName);
        Assert.AreEqual(asset.SerialNumber, result.SerialNumber);
        Assert.AreEqual(asset.AssignedStoreId, result.AssignedStoreId);
        Assert.AreEqual(asset.AssignedUserId, result.AssignedUserId);
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Null_When_Asset_Does_Not_Exist()
    {
        // Act
        var result = await _repository.GetByIdAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Untracked_Asset()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var asset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "ID-TRACKING-001");

        await SeedAssetsAsync(asset);

        // Act
        var result = await _repository.GetByIdAsync(
            asset.Id,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);

        Assert.AreEqual(
            EntityState.Detached,
            _dbContext.Entry(result).State);
    }

    [TestMethod]
    public async Task GetBySerialNumberAsync_Should_Return_Asset_When_Asset_Exists()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var asset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "SERIAL-001");

        await SeedAssetsAsync(asset);

        // Act
        var result = await _repository.GetBySerialNumberAsync(
            "SERIAL-001",
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(asset.Id, result.Id);
        Assert.AreEqual("SERIAL-001", result.SerialNumber);
    }

    [TestMethod]
    public async Task GetBySerialNumberAsync_Should_Return_Null_When_Asset_Does_Not_Exist()
    {
        // Act
        var result = await _repository.GetBySerialNumberAsync(
            "UNKNOWN-SERIAL",
            CancellationToken.None);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetBySerialNumberAsync_Should_Return_Untracked_Asset()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var asset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "SERIAL-TRACKING-001");

        await SeedAssetsAsync(asset);

        // Act
        var result = await _repository.GetBySerialNumberAsync(
            asset.SerialNumber,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);

        Assert.AreEqual(
            EntityState.Detached,
            _dbContext.Entry(result).State);
    }

    [TestMethod]
    public async Task GetByAssignedUserIdAsync_Should_Return_Assigned_Assets()
    {
        // Arrange
        var store = new StoreBuilder().Build();
        var firstUser = new ApplicationUserBuilder().Build();
        var secondUser = new ApplicationUserBuilder().Build();

        _dbContext.Stores.Add(store);
        _dbContext.ApplicationUsers.AddRange(
            firstUser,
            secondUser);

        await _dbContext.SaveChangesAsync();

        var firstAssignedAsset = CreateAsset(
            store.Id,
            firstUser.Id,
            serialNumber: "USER-001");

        var secondAssignedAsset = CreateAsset(
            store.Id,
            firstUser.Id,
            serialNumber: "USER-002");

        var otherUserAsset = CreateAsset(
            store.Id,
            secondUser.Id,
            serialNumber: "USER-003");

        await SeedAssetsAsync(
            firstAssignedAsset,
            secondAssignedAsset,
            otherUserAsset);

        // Act
        var result =
            await _repository.GetByAssignedUserIdAsync(
                firstUser.Id,
                CancellationToken.None);

        // Assert
        Assert.HasCount(2, result);

        Assert.IsTrue(result.All(asset =>
            asset.AssignedUserId == firstUser.Id));

        Assert.IsTrue(result.Any(asset =>
            asset.Id == firstAssignedAsset.Id));

        Assert.IsTrue(result.Any(asset =>
            asset.Id == secondAssignedAsset.Id));

        Assert.IsFalse(result.Any(asset =>
            asset.Id == otherUserAsset.Id));
    }

    [TestMethod]
    public async Task GetByAssignedUserIdAsync_Should_Return_Empty_List_When_User_Has_No_Assets()
    {
        // Act
        var result =
            await _repository.GetByAssignedUserIdAsync(
                Guid.NewGuid(),
                CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task GetByAssignedUserIdAsync_Should_Return_Untracked_Assets()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var asset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "USER-TRACKING-001");

        await SeedAssetsAsync(asset);

        // Act
        var result =
            await _repository.GetByAssignedUserIdAsync(
                testData.User.Id,
                CancellationToken.None);

        var returnedAsset = result.Single();

        // Assert
        Assert.AreEqual(
            EntityState.Detached,
            _dbContext.Entry(returnedAsset).State);
    }

    [TestMethod]
    public async Task GetForUpdateByIdAsync_Should_Return_Tracked_Asset_When_Asset_Exists()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var asset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "UPDATE-001");

        await SeedAssetsAsync(asset);

        // Act
        var result =
            await _repository.GetForUpdateByIdAsync(
                asset.Id,
                CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(asset.Id, result.Id);

        Assert.AreEqual(
            EntityState.Unchanged,
            _dbContext.Entry(result).State);
    }

    [TestMethod]
    public async Task GetForUpdateByIdAsync_Should_Return_Null_When_Asset_Does_Not_Exist()
    {
        // Act
        var result =
            await _repository.GetForUpdateByIdAsync(
                Guid.NewGuid(),
                CancellationToken.None);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetForUpdateByIdAsync_Should_Persist_Changes_When_Tracked_Asset_Is_Modified()
    {
        // Arrange
        var store = new StoreBuilder().WithStoreNumber("321").Build();
        var targetStore = new StoreBuilder().WithStoreNumber("322").Build();
        var user = new ApplicationUserBuilder().Build();

        _dbContext.Stores.AddRange(store, targetStore);
        _dbContext.ApplicationUsers.Add(user);

        await _dbContext.SaveChangesAsync();

        var asset = CreateAsset(
            store.Id,
            user.Id,
            serialNumber: "UPDATE-PERSIST-001");

        await SeedAssetsAsync(asset);

        var trackedAsset =
            await _repository.GetForUpdateByIdAsync(
                asset.Id,
                CancellationToken.None);

        Assert.IsNotNull(trackedAsset);

        // Act
        trackedAsset.TransferToStore(targetStore.Id);

        await _repository.SaveChangesAsync(
            CancellationToken.None);

        _dbContext.ChangeTracker.Clear();

        // Assert
        var persistedAsset = await _dbContext.Assets
            .AsNoTracking()
            .SingleAsync(candidate =>
                candidate.Id == asset.Id);

        Assert.AreEqual(
            targetStore.Id,
            persistedAsset.AssignedStoreId);

        Assert.IsNull(persistedAsset.AssignedUserId);
    }

    [TestMethod]
    public async Task SaveChangesAsync_Should_Throw_When_SerialNumber_Is_Duplicated()
    {
        // Arrange
        var testData = await SeedRequiredEntitiesAsync();

        var firstAsset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "DUPLICATE-001");

        var secondAsset = CreateAsset(
            testData.Store.Id,
            testData.User.Id,
            serialNumber: "DUPLICATE-001");

        await _repository.AddAsync(
            firstAsset,
            CancellationToken.None);

        await _repository.SaveChangesAsync(
            CancellationToken.None);

        await _repository.AddAsync(
            secondAsset,
            CancellationToken.None);

        // Act and Assert
        await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => _repository.SaveChangesAsync(
                CancellationToken.None));
    }

    [TestMethod]
    public async Task AddAsync_Should_Throw_When_Asset_Is_Null()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _repository.AddAsync(
                null!,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task GetAllAsync_Should_Respect_Cancelled_CancellationToken()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => _repository.GetAllAsync(
                cancellationTokenSource.Token));
    }

    private async Task<AssetTestData> SeedRequiredEntitiesAsync()
    {
        var store = new StoreBuilder().Build();
        var user = new ApplicationUserBuilder().Build();

        _dbContext.Stores.Add(store);
        _dbContext.ApplicationUsers.Add(user);

        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        return new AssetTestData(store, user);
    }

    private async Task SeedAssetsAsync(
        params Asset[] assets)
    {
        _dbContext.Assets.AddRange(assets);

        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();
    }

    private static Asset CreateAsset(
        Guid assignedStoreId,
        Guid? assignedUserId,
        string serialNumber)
    {
        return Asset.Create(
            assetName: $"Asset-{serialNumber}",
            assetType: AssetType.Notebook,
            assetStatus: AssetStatus.In_HQ,
            manufacturer: "Lenovo",
            model: "E14",
            serialNumber: serialNumber,
            rfidTagId: "1234",
            assignedStoreId: assignedStoreId,
            assignedUserId: assignedUserId,
            macAddress: null,
            imei: null,
            operatingSystem: "Incom OS",
            operatingSystemVersion: "v7");
    }

    private sealed record AssetTestData(
        Store Store,
        ApplicationUser User);
}
