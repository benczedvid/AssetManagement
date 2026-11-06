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

            var result = await _userRepository.GetByEntraIdentityAsync(
                unknownObjectId,
                unknownTenantId);

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

            var result = await _userRepository.GetByEntraIdentityAsync(tenantId, otherObjectId);

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
    }
}
