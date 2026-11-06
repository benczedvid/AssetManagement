using AssetManagement.Infrastructure.Persistence;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AssetManagement.WebApi.IntegrationTests.Infrastructure
{
    internal sealed class AssetManagementWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private SqliteConnection? _connection;

        private readonly string _databaseName =
            $"DeathStarIntegrationTests_{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTest");

            builder.ConfigureAppConfiguration(
                (_, configurationBuilder) =>
                {
                    var testConfiguration =
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:AssetManagementDatabase"] = "Server-integration-test",
                            ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                            ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000000",
                            ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000001",
                            ["AzureAd:Audience"] = "api://00000000-0000-0000-0000-000000000001",
                            ["AzureAd:Scopes"] = "access_as_user",
                            ["MicrosoftGraphEmail:TenantId"] = "00000000-0000-0000-0000-000000000000",
                            ["MicrosoftGraphEmail:ClientId"] = "00000000-0000-0000-0000-000000000000",
                            ["MicrosoftGraphEmail:ClientSecret"] = "integration-test-client-secret",
                            ["MicrosoftGraphEmail:SendUserPrincipalName"] = "test@praktiker.hu"
                        };

                    configurationBuilder.AddInMemoryCollection(testConfiguration);
                });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

                _connection = new SqliteConnection(
                    $"Data Source={_databaseName};" +
                    "Mode=Memory;" +
                    "Cache=Shared");

                _connection.Open();

                services.AddSingleton(_connection);
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultScheme = TestAuthenticationHandler.AuthenticationScheme;
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                        options.DefaultForbidScheme = TestAuthenticationHandler.AuthenticationScheme;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme,
                        _ => { }
                        );
            });
        }

        protected override IHost CreateHost(
            IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Database.EnsureCreated();

            return host;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection?.Dispose();
                _connection = null;
            }

            base.Dispose(disposing);
        }
    }
}