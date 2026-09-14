using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Infrastructure;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetManagement.Tests.Infrastructure
{
    [TestClass]
    public class DependencyInjectionTests
    {
        public void AddInfrastructure_Should_Register_ApplicationUserRepository()
        {
            var configurationValues =
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AppDatabase"] =
                        "Server=(localdb)\\MSSQLLocalDB;" +
                        "Database=DeathStarDependencyInjectionTests;" +
                        "Trusted_Connection=True;" +
                        "TrustServerCertificate=True;"
                };
            var configuration =
                new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();
            var services = new ServiceCollection();

            services.AddInfrastructure(configuration);
            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var repository =
                scope.ServiceProvider
                .GetService<IApplicationUserRepository>();

            var dbContext = scope.ServiceProvider
                .GetService<AppDbContext>();

            Assert.IsNotNull(repository);
            Assert.IsInstanceOfType<ApplicationUserRepository>(repository);
            Assert.IsNotNull(dbContext);
        }

        [TestMethod]
        public void AddInfrastructure_Should_Throw_When_ConnenctionString_Is_Missing()
        {
            var configuration = new ConfigurationBuilder().Build();

            var services = new ServiceCollection();

            Assert.ThrowsExactly<InvalidOperationException>(() => services.AddInfrastructure(configuration));
        }
    }
}
