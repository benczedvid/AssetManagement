using AssetManagement.Application.Users.GetApplicationUsers;
using AssetManagement.Tests.Builders;
using AssetManagement.Tests.Fakes;

namespace AssetManagement.Tests.Application.Users.GetApplicationUsers
{
    [TestClass]
    public sealed class GetApplicationUsersHandlerTests
    {
        private FakeApplicationUserRepository _repository = null!;
        private GetApplicationUsersHandler _handler = null!;

        [TestInitialize]
        public void Initialize()
        {
            _repository = new FakeApplicationUserRepository();
            _handler = new GetApplicationUsersHandler(_repository);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new GetApplicationUsersHandler(null!));

            Assert.AreEqual("applicationUserRepository", exception.ParamName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Empty_List_When_No_Users_Exist()
        {
            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.IsEmpty(response);
            Assert.AreEqual(1, _repository.GetAllCallCount);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Return_Id_And_DisplayName_Of_All_Users()
        {
            var firstUser = new ApplicationUserBuilder()
                .WithDisplayName("John Doe")
                .Build();

            var secondUser = new ApplicationUserBuilder()
                .WithDisplayName("Jane Smith")
                .AsInactive()
                .Build();

            _repository.Seed(firstUser, secondUser);

            var response = await _handler.HandleAsync(CancellationToken.None);

            Assert.HasCount(2, response);
            Assert.AreEqual("John Doe", response.Single(user => user.Id == firstUser.Id).DisplayName);
            Assert.AreEqual("Jane Smith", response.Single(user => user.Id == secondUser.Id).DisplayName);
        }

        [TestMethod]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Repository()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            await _handler.HandleAsync(cancellationToken);

            Assert.AreEqual(cancellationToken, _repository.LastGetAllCancellationToken);
        }
    }
}
