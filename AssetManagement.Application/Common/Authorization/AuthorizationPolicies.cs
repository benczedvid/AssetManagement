namespace AssetManagement.Application.Common.Authorization
{
    public static class AuthorizationPolicies
    {
        public const string ViewDashboard = nameof(ViewDashboard);
        public const string ViewAssets = nameof(ViewAssets);
        public const string ViewEmployees = nameof(ViewEmployees);
        public const string ManageAssets = nameof(ManageAssets);
        public const string ManageStores = nameof(ManageStores);
        public const string ManageUsers = nameof(ManageUsers);
        public const string ManageVendors = nameof(ManageVendors);

        public const string ViewServiceCases = nameof(ViewServiceCases);
        public const string ManageServiceCases = nameof(ManageServiceCases);
        public const string ManageAllServiceCases = nameof(ManageAllServiceCases);
        public const string CloseServiceCases = nameof(CloseServiceCases);
        /// <summary>
        /// Policy for viewing packages.
        /// </summary>
        public const string ViewPackages = "ViewPackages";

        /// <summary>
        /// Policy for package modification operations.
        /// </summary>
        public const string ManagePackages =  "ManagePackages";
            }
}