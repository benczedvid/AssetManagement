using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Email;
using AssetManagement.Application.Common.Interfaces.Employees;
using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Common.Interfaces.Vendors;
using AssetManagement.Infrastructure.Email;
using AssetManagement.Infrastructure.Identity;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Infrastructure.Persistence.Repositories;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace AssetManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString =
            configuration.GetConnectionString("DeathStarDatabase")
            ?? throw new InvalidOperationException(
                "The database connection string is missing.");

        services.AddDbContext<AppDbContext>(
            options => options.UseSqlServer(connectionString));

        RegisterRepositories(services);
        RegisterMicrosoftGraph(services, configuration);

        services.AddScoped<IEmailSender, MicrosoftGraphEmailSender>();

        services.AddScoped<
            IEntraUserProfileService,
            MicrosoftGraphEntraUserProfileService>();

        return services;
    }

    private static void RegisterRepositories(
        IServiceCollection services)
    {
        services.AddScoped<
            IApplicationUserRepository,
            ApplicationUserRepository>();

        services.AddScoped<
            IStoreRepository,
            StoreRepository>();

        services.AddScoped<
            IAssetRepository,
            AssetRepository>();

        services.AddScoped<
            IEmployeeRepository,
            EmployeeRepository>();

        services.AddScoped<
            IAssetMovementRepository,
            AssetMovementRepository>();

        services.AddScoped<
            IUnreturnedEmployeeAssetReportRepository,
            UnreturnedEmployeeAssetReportRepository>();

        services.AddScoped<
            IVendorRepository,
            VendorRepository>();
        services.AddScoped<
    IEntraUserProfileService,
    MicrosoftGraphEntraUserProfileService>();
    }

    private static void RegisterMicrosoftGraph(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MicrosoftGraphEmailOptions>()
            .Bind(
                configuration.GetSection(
                    MicrosoftGraphEmailOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.TenantId),
                "MicrosoftGraphEmail:TenantId is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.ClientId),
                "MicrosoftGraphEmail:ClientId is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.ClientSecret),
                "MicrosoftGraphEmail:ClientSecret is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.SendUserPrincipalName),
                "MicrosoftGraphEmail:SendUserPrincipalName is required.")
            .ValidateOnStart();

        services.AddSingleton<GraphServiceClient>(
            serviceProvider =>
            {
                var options = serviceProvider
                    .GetRequiredService<
                        IOptions<MicrosoftGraphEmailOptions>>()
                    .Value;

                var credential = new ClientSecretCredential(
                    tenantId: options.TenantId,
                    clientId: options.ClientId,
                    clientSecret: options.ClientSecret);

                return new GraphServiceClient(
                    credential,
                    ["https://graph.microsoft.com/.default"]);
            });
    }
}