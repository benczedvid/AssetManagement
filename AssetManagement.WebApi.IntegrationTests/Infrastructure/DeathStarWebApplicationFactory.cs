using AssetManagement.Infrastructure.Persistence;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;


namespace AssetManagement.WebApi.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Creates a test host for DeathStar Web API integration tests.
    /// </summary>
    internal sealed class DeathStarWebApplicationFactory : WebApplicationFactory<Program>
    {
        private SqliteConnection _connection = null!;
        private readonly string _databaseName = $"DeathStarIntegrationTests_{Guid.NewGuid():N}";


        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTest");

            builder.ConfigureAppConfiguration((_, ConfigurationBuilder) =>
            {
                var testConfiguration = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DeathStarDatabase"] = "Server-integration-test",
                    ["AzureAd:Instance"] = "https://login.microsoft.com",
                    ["AzureAd:TenantId"] = Guid.Empty.ToString(),
                    ["AzureAd:ClientId"] = Guid.Empty.ToString(),
                    ["AzureAd:Audience"] = $"api://{Guid.Empty}",
                    ["AzureAd:Scopes"] = "access_as_user"
                };
                ConfigurationBuilder.AddInMemoryCollection(testConfiguration);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                _connection = new SqliteConnection($"Data Source={_databaseName};Mode=Memory;Cache=Shared");
                _connection.Open();

                services.AddSingleton(_connection);
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultScheme = TestAuthenticationHandler.AuthenticationScheme;
                    }
                ).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme, _ => { });
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();
            return host;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
