namespace AssetManagement.Application.Common.Authorization
{
    public static class AuthorizationPolicies
    {
        public const string ViewDashboard =
            nameof(ViewDashboard);

        public const string ViewAssets =
            nameof(ViewAssets);

        public const string ViewEmployees =
            nameof(ViewEmployees);

        public const string ManageAssets =
            nameof(ManageAssets);

        public const string ManageStores =
            nameof(ManageStores);

        public const string ManageUsers =
            nameof(ManageUsers);
    }
}