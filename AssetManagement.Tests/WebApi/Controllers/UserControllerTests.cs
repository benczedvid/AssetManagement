using AssetManagement.Application.Users.GetOrCreateCurrentUser;
using AssetManagement.Tests.Fakes;
using AssetManagement.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Tests.WebApi.Controllers
{
    [TestClass]
    public class UserControllerTests
    {
        [TestMethod]
        public async Task GetMe_Should_Return_Ok_With_Current_User()
        {
            var currentUser = new FakeCurrentUser
            {
                IsAuthenticated = true,
                EntraObjectId = Guid.NewGuid(),
                EntraTenantId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                DisplayName = "John Doe"
            };
            var userRepository = new FakeApplicationUserRepository();
            var employeeRepository = new FakeEmployeeRepository();
            var entraUserProfileService = new FakeEntraUserProfileService();
            var storeRepository = new FakeStoreRepository();
            var handler = new GetOrCreateCurrentUserHandler(currentUser, userRepository, entraUserProfileService, employeeRepository, storeRepository);
            var controller = new UsersController(handler);

            var actionResult = await controller.GetMe(CancellationToken.None);

            var okResult = actionResult.Result as OkObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var response = okResult.Value as CurrentUserResponse;
            Assert.IsNotNull(response);
            Assert.AreEqual(currentUser.EntraObjectId, response.EntraObjectId);
            Assert.AreEqual(currentUser.EntraTenantId, response.EntraTenantId);
            Assert.AreEqual(currentUser.FirstName, response.FirstName);
            Assert.AreEqual(currentUser.LastName, response.LastName);
            Assert.AreEqual(currentUser.DisplayName, response.DisplayName);
            Assert.IsTrue(response.IsActive);
        }

        [TestMethod]
        public void Constructor_Should_Throw_When_Handler_Is_Null()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new UsersController(null!));
        }
    }
}
