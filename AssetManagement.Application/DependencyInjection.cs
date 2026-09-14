using AssetManagement.Application.AssetMovements.AssetMovementReports;
using AssetManagement.Application.AssetMovements.CreateAssetMovement;
using AssetManagement.Application.Assets.CreateAsset;
using AssetManagement.Application.Assets.GetAsset;
using AssetManagement.Application.Assets.GetAssetDetails;
using AssetManagement.Application.Assets.GetAssets;
using AssetManagement.Application.Assets.UpdateAsset;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Application.Dashboard.GetStoreDashboard;
using AssetManagement.Application.Employees.GetEmployeeByEmployeeNumber;
using AssetManagement.Application.Employees.GetEmployees;
using AssetManagement.Application.Stores.CreateStore;
using AssetManagement.Application.Stores.GetStoreById;
using AssetManagement.Application.Stores.GetStores;
using AssetManagement.Application.Stores.UpdateStore;
using AssetManagement.Application.Users.GetOrCreateCurrentUser;
using AssetManagement.Application.Vendors.CreateVendor;
using AssetManagement.Application.Vendors.GetVendorById;
using AssetManagement.Application.Vendors.GetVendors;
using AssetManagement.Application.Vendors.UpdateVendor;
using Microsoft.Extensions.DependencyInjection;

namespace AssetManagement.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication( this IServiceCollection services)
        {
            services.AddScoped<GetOrCreateCurrentUserHandler>();

            services.AddScoped<CreateStoreHandler>();
            services.AddScoped<GetStoreByIdHandler>();
            services.AddScoped<GetStoresHandler>();
            services.AddScoped<UpdateStoreHandler>();

            services.AddScoped<CreateAssetHandler>();
            services.AddScoped<GetAssetsHandler>();
            services.AddScoped<GetAssetByIdHandler>();
            services.AddScoped<UpdateAssetHandler>();
            services.AddScoped<GetAssetDetailsHandler>();

            services.AddScoped<GetStoreDashboardHandler>();

            services.AddScoped<GetEmployeeByEmployeeNumberHandler>();
            services.AddScoped<GetEmployeesHandler>();

            services.AddScoped<CreateAssetMovementHandler>();


            services.AddScoped<CreateVendorHandler>();
            services.AddScoped<GetVendorByIdHandler>();
            services.AddScoped<GetVendorsHandler>();
            services.AddScoped<UpdateVendorHandler>();

            
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<IEndOfDayAssetReportService, EndOfDayAssetReportService>();

            services.AddScoped<IUserAccessScopeResolver, UserAccessScopeResolver>();
            return services;
        }
    }
}
