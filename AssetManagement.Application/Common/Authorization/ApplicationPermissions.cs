namespace AssetManagement.Application.Common.Authorization;

public static class ApplicationPermissions
{
    public const string DashboardReadAll =
        "Dashboard.Read.All";

    public const string DashboardReadOwnStore =
        "Dashboard.Read.OwnStore";

    public const string AssetsReadAll =
        "Assets.Read.All";

    public const string AssetsReadOwnStore =
        "Assets.Read.OwnStore";

    public const string EmployeesReadAll =
        "Employees.Read.All";

    public const string EmployeesReadOwnStore =
        "Employees.Read.OwnStore";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal)
        {
            DashboardReadAll,
            DashboardReadOwnStore,
            AssetsReadAll,
            AssetsReadOwnStore,
            EmployeesReadAll,
            EmployeesReadOwnStore
        };
}
