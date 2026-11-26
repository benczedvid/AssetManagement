using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Infrastructure.Persistence.Repositories;
using AssetManagement.Tests.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Tests.Infrastructure.Persistence.Repositories
{
    [TestClass]
    public class ApplicationUserRepositoryTests
    {
        private SqliteConnection _sqliteConnection = null!;
        private AppDbContext _appDbContext = null!;
        private ApplicationUserRepository _userRepository = null!;

        [TestInitialize]
        public void Initialize()
        {
            _sqliteConnection = new SqliteConnection("Data Source=:memory:");
            _sqliteConnection.Open();

            var options =
                new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_sqliteConnection).Options;

            _appDbContext = new AppDbContext(options);
            _appDbContext.Database.EnsureCreated();
            _userRepository = new ApplicationUserRepository(_appDbContext);
        }
        [TestCleanup]
        public void Cleanup()
        {
            _appDbContext.Dispose();
            _sqliteConnection.Dispose();
        }

        [TestMethod]
        public async Task AddAsync_And_SaveChangesAsync_Should_Persist_User()
        {
            var user = new ApplicationUserBuilder().Build();

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var persistedUser = await _appDbContext.ApplicationUsers.SingleOrDefaultAsync(candidate => candidate.Id == user.Id);

            Assert.IsNotNull(persistedUser);

            Assert.AreEqual(user.EntraObjectId, persistedUser!.EntraObjectId);
            Assert.AreEqual(user.EntraTenantId, persistedUser.EntraTenantId);
            Assert.AreEqual(user.DisplayName, persistedUser.DisplayName);
        }

        [TestMethod]
        public async Task GetByEntraIdentityAsync_Should_Return_Matching_User()
        {
            var user = new ApplicationUserBuilder().Build();

            _appDbContext.ApplicationUsers.Add(user);
            await _appDbContext.SaveChangesAsync();

            var result = await _userRepository.GetByEntraIdentityAsync(
                user.EntraObjectId,
                user.EntraTenantId);

            Assert.IsNotNull(result);
            Assert.AreEqual(user.Id, result.Id);
            Assert.AreEqual(user.EntraObjectId, result.EntraObjectId);
            Assert.AreEqual(user.EntraTenantId, result.EntraTenantId);
        }
        [TestMethod]
        public async Task GetByIdAsync_Should_Return_Matching_User()
        {
            var user = new ApplicationUserBuilder().Build();

            _appDbContext.ApplicationUsers.Add(user);
            await _appDbContext.SaveChangesAsync();

            var result = await _userRepository.GetByIdAsync(user.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(user.Id, result.Id);
            Assert.AreEqual(user.EntraObjectId, result.EntraObjectId);
            Assert.AreEqual(user.EntraTenantId, result.EntraTenantId);
        }

        [TestMethod]
        public async Task GetByEntraIdentityAsync_Should_Return_Null_When_User_Does_Not_Exist()
        {
            var unknownObjectId = Guid.NewGuid();
            var unknownTenantId = Guid.NewGuid();

            var result = await _userRepository.GetByEntraIdentityAsync(unknownObjectId, unknownTenantId);

            Assert.IsNull(result);
        }
        [TestMethod]
        public async Task GetByEntraIdentityAsync_Should_Not_Return_When_User_From_Different_Tenant()
        {
            var objectId = Guid.NewGuid();

            var existingUser = new ApplicationUserBuilder()
                .WithEntraObjectId(objectId)
                .WithEntraTenantId(Guid.NewGuid())
                .Build();

            _appDbContext.ApplicationUsers.Add(existingUser);

            await _appDbContext.SaveChangesAsync();

            var otherTenantId = Guid.NewGuid();
            var result = await _userRepository.GetByEntraIdentityAsync(objectId, otherTenantId);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetByEntraIdentityAsync_Should_Not_Return_User_With_Different_ObjectId()
        {
            var tenantId = Guid.NewGuid();
            var existingUser = new ApplicationUserBuilder()
                .WithEntraTenantId(tenantId)
                .WithEntraObjectId(Guid.NewGuid())
                .Build();

            _appDbContext.ApplicationUsers.Add(existingUser);
            await _appDbContext.SaveChangesAsync();

            var otherObjectId = Guid.NewGuid();
            var result =
            await _userRepository.GetByEntraIdentityAsync(otherObjectId, tenantId);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SaveChangesAsync_Should_Throw_When_EntraIdentity_Is_Duplicated()
        {
            var objectId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            var firstUser = new ApplicationUserBuilder()
                .WithEntraObjectId(objectId)
                .WithEntraTenantId(tenantId)
                .Build();

            var secondUser = new ApplicationUserBuilder()
                .WithEntraObjectId(objectId)
                .WithEntraTenantId(tenantId)
                .Build();


            _appDbContext.ApplicationUsers.Add(firstUser);
            await _appDbContext.SaveChangesAsync();
            _appDbContext.ApplicationUsers.Add(secondUser);

            await Assert.ThrowsExactlyAsync<DbUpdateException>(() => _appDbContext.SaveChangesAsync());
        }

        [TestMethod]
        public async Task GetAllAsync_Should_Return_All_Active_Users()
        {
            var firstUser = new ApplicationUserBuilder()
                .WithDisplayName("Luke Skywalker")
                .Build();

            var secondUser = new ApplicationUserBuilder()
                .WithDisplayName("Leia Organa")
                .Build();

            _appDbContext.ApplicationUsers.AddRange(firstUser, secondUser);

            await _appDbContext.SaveChangesAsync();
            _appDbContext.ChangeTracker.Clear();

            var result = await _userRepository.GetAllAsync(CancellationToken.None);

            Assert.HasCount(2, result);
            Assert.Contains(user => user.Id == firstUser.Id, result);
            Assert.Contains(user => user.Id == secondUser.Id, result);
        }

        [TestMethod]
        public async Task GetAllAsync_Should_Return_Empty_List_When_No_Users_Exist()
        {
            var result = await _userRepository.GetAllAsync(CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [TestMethod]
        public async Task GetAllAsync_Should_Order_Users_By_DisplayName()
        {
            var secondUser = new ApplicationUserBuilder()
                .WithDisplayName("Luke Skywalker")
                .Build();

            var firstUser = new ApplicationUserBuilder()
                .WithDisplayName("Leia Organa")
                .Build();

            _appDbContext.ApplicationUsers.AddRange(secondUser, firstUser);

            await _appDbContext.SaveChangesAsync();
            _appDbContext.ChangeTracker.Clear();

            var result = await _userRepository.GetAllAsync(CancellationToken.None);

            Assert.HasCount(2, result);
            Assert.AreEqual("Leia Organa", result[0].DisplayName);
            Assert.AreEqual("Luke Skywalker", result[1].DisplayName);
        }

        [TestMethod]
        public async Task GetAllAsync_Should_Return_Untracked_Users()
        {
            var user = new ApplicationUserBuilder().Build();

            _appDbContext.ApplicationUsers.Add(user);
            await _appDbContext.SaveChangesAsync();
            _appDbContext.ChangeTracker.Clear();

            var result = await _userRepository.GetAllAsync(CancellationToken.None);
            var returnedUser = result.Single();

            Assert.AreEqual(EntityState.Detached, _appDbContext.Entry(returnedUser).State);
        }

        [TestMethod]
        public async Task GetAllAsync_Should_Respect_Cancelled_CancellationToken()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => _userRepository.GetAllAsync(cancellationTokenSource.Token));
        }
    }
}
