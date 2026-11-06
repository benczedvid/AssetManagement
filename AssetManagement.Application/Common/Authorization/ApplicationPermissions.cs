namespace AssetManagement.Application.Common.Authorization;

public static class ApplicationPermissions
{
    public const string DashboardReadAll = "Dashboard.Read.All";
    public const string DashboardReadOwnStore = "Dashboard.Read.OwnStore";
    public const string AssetsReadAll = "Assets.Read.All";
    public const string AssetsReadOwnStore = "Assets.Read.OwnStore";
    public const string EmployeesReadAll = "Employees.Read.All";
    public const string EmployeesReadOwnStore = "Employees.Read.OwnStore";
    public const string ServiceCasesReadAll = "ServiceCases.Read.All";
    public const string ServiceCasesReadOwnStore = "ServiceCases.Read.OwnStore";
    public const string ServiceCasesManageAll = "ServiceCases.Manage.All";
    public const string ServiceCasesManageOwnStore = "ServiceCases.Manage.OwnStore";
    public const string ServiceCasesClose = "ServiceCases.Close";

    /// <summary>
    /// Allows reading every package regardless of store or status.
    /// </summary>
    public const string PackagesReadAll = "Packages.ReadAll";

    /// <summary>
    /// Allows reading packages involving the user's assigned store.
    /// </summary>
    public const string PackagesReadOwnStore = "Packages.ReadOwnStore";

    /// <summary>
    /// Allows managing every package.
    /// </summary>
    public const string PackagesManageAll = "Packages.ManageAll";

    /// <summary>
    /// Allows managing packages involving the user's assigned store.
    /// </summary>
    public const string PackagesManageOwnStore = "Packages.ManageOwnStore";
    public static readonly IReadOnlySet<string> All =  new HashSet<string>(StringComparer.Ordinal)
        {
            DashboardReadAll,
            DashboardReadOwnStore,
            AssetsReadAll,
            AssetsReadOwnStore,
            EmployeesReadAll,
            EmployeesReadOwnStore,
            ServiceCasesReadAll,
            ServiceCasesReadOwnStore,
            ServiceCasesManageAll,
            ServiceCasesManageOwnStore,
            ServiceCasesClose,
            PackagesReadAll,
            PackagesReadOwnStore,
            PackagesManageAll,
            PackagesManageOwnStore
        };
}
