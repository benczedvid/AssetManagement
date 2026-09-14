using AssetManagement.Application;
using AssetManagement.Application.Users.GetOrCreateCurrentUser;
using Microsoft.Extensions.DependencyInjection;

namespace AssetManagement.Tests.Application
{
    [TestClass]
    public class DependencyInjectionTests
    {
        [TestMethod]
        public void AddApplication_Should_Register_GetOrCreateCurrentUserHandler()
        {
            var services = new ServiceCollection();

            services.AddApplication();

            var descriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(GetOrCreateCurrentUserHandler));

            Assert.IsNotNull(descriptor);
            Assert.AreEqual(
                ServiceLifetime.Scoped,
                descriptor.Lifetime);
            Assert.AreEqual(
                typeof(GetOrCreateCurrentUserHandler),
                descriptor.ImplementationType);
        }
    }
}
