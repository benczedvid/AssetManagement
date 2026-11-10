using AssetManagement.Application.AssetHistories.GetAssetHistory;
using AssetManagement.Application.AssetMovements.AssetMovementReports;
using AssetManagement.Application.AssetMovements.CreateAssetMovement;
using AssetManagement.Application.Assets.CreateAsset;
using AssetManagement.Application.Assets.DisposeAsset;
using AssetManagement.Application.Assets.GetAsset;
using AssetManagement.Application.Assets.GetAssetDetails;
using AssetManagement.Application.Assets.GetAssets;
using AssetManagement.Application.Assets.UpdateAsset;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Application.Dashboard.GetStoreDashboard;
using AssetManagement.Application.Employees.GetEmployeeByEmployeeNumber;
using AssetManagement.Application.Employees.GetEmployees;
using AssetManagement.Application.Packages.AddPackageAsset;
using AssetManagement.Application.Packages.AssignCourier;
using AssetManagement.Application.Packages.CancelPackage;
using AssetManagement.Application.Packages.CreatePackage;
using AssetManagement.Application.Packages.DispatchPackage;
using AssetManagement.Application.Packages.GetPackageById;
using AssetManagement.Application.Packages.GetPackages;
using AssetManagement.Application.Packages.ReceivePackage;
using AssetManagement.Application.Packages.RemovePackageAsset;
using AssetManagement.Application.ServiceCases.CloseServiceCase;
using AssetManagement.Application.ServiceCases.CompleteRepairAtHeadquarters;
using AssetManagement.Application.ServiceCases.ConfirmReceivedByExternalService;
using AssetManagement.Application.ServiceCases.DispatchToExternalService;
using AssetManagement.Application.ServiceCases.DisposeServiceCaseAsset;
using AssetManagement.Application.ServiceCases.GetServiceCaseDetails;
using AssetManagement.Application.ServiceCases.GetServiceCases;
using AssetManagement.Application.ServiceCases.HandOverToExternalService;
using AssetManagement.Application.ServiceCases.OpenServiceCase;
using AssetManagement.Application.ServiceCases.RequireExternalService;
using AssetManagement.Application.ServiceCases.ResolveAfterExternalServiceReturn;
using AssetManagement.Application.ServiceCases.ReturnFromExternalService;
using AssetManagement.Application.ServiceDesk.GetServiceDeskSyncOperations;
using AssetManagement.Application.ServiceDesk.ReconcileServiceDeskRequest;
using AssetManagement.Application.Stores.CreateStore;
using AssetManagement.Application.Stores.GetStoreById;
using AssetManagement.Application.Stores.GetStores;
using AssetManagement.Application.Stores.UpdateStore;
using AssetManagement.Application.Users.GetApplicationUsers;
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
            services.AddScoped<GetApplicationUsersHandler>();

            services.AddScoped<CreateStoreHandler>();
            services.AddScoped<GetStoreByIdHandler>();
            services.AddScoped<GetStoresHandler>();
            services.AddScoped<UpdateStoreHandler>();

            services.AddScoped<CreateAssetHandler>();
            services.AddScoped<GetAssetsHandler>();
            services.AddScoped<GetAssetByIdHandler>();
            services.AddScoped<UpdateAssetHandler>();
            services.AddScoped<GetAssetDetailsHandler>();
            services.AddScoped<DisposeAssetHandler>();

            services.AddScoped<GetStoreDashboardHandler>();

            services.AddScoped<GetEmployeeByEmployeeNumberHandler>();
            services.AddScoped<GetEmployeesHandler>();

            services.AddScoped<CreateAssetMovementHandler>();


            services.AddScoped<CreateVendorHandler>();
            services.AddScoped<GetVendorByIdHandler>();
            services.AddScoped<GetVendorsHandler>();
            services.AddScoped<UpdateVendorHandler>();

            services.AddScoped<OpenServiceCaseHandler>();
            services.AddScoped<RequireExternalServiceHandler>();
            services.AddScoped<DispatchToExternalServiceHandler>();
            services.AddScoped<HandOverToExternalServiceHandler>();
            services.AddScoped<ConfirmReceivedByExternalServiceHandler>();
            services.AddScoped<ReturnFromExternalServiceHandler>();
            services.AddScoped<CompleteRepairAtHeadquartersHandler>();
            services.AddScoped<ResolveAfterExternalServiceReturnHandler>();
            services.AddScoped<CloseServiceCaseHandler>();
            services.AddScoped<GetServiceCasesHandler>();
            services.AddScoped<GetServiceCaseDetailsHandler>();
            services.AddScoped<DisposeServiceCaseAssetHandler>();
            
            services.AddScoped<ReconcileServiceDeskRequestHandler>();
            services.AddScoped<GetServiceDeskSyncOperationsHandler>();

            services.AddSingleton(TimeProvider.System);
            services.AddScoped<IDailyAssetReportService, StartOfDayAssetReportService>();
            services.AddScoped<IDailyAssetReportService, EndOfDayAssetReportService>();
            services.AddScoped<IOverdueAssetReportService, OverdueAssetReportService>();

            services.AddScoped<IUserAccessScopeResolver, UserAccessScopeResolver>();
            services.AddScoped<GetAssetHistoryHandler>();

            services.AddScoped<CreatePackageHandler>();
            services.AddScoped<GetPackageByIdHandler>();
            services.AddScoped<GetPackagesHandler>();
            services.AddScoped<AssignCourierHandler>();
            services.AddScoped<DispatchPackageHandler>();
            services.AddScoped<ReceivePackageHandler>();
            services.AddScoped<CancelPackageHandler>();
            services.AddScoped<AddPackageAssetHandler>();
            services.AddScoped<RemovePackageAssetHandler>();

            return services;
        }


    }
}
