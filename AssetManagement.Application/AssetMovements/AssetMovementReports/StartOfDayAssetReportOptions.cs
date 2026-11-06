namespace AssetManagement.Application.AssetMovements.AssetMovementReports
{
    public sealed class StartOfDayAssetReportOptions
    {
        public const string SectionName = "StartOfDayAssetReport";
        public bool Enabled { get; init; }
        public string CronExpression { get; init; } = string.Empty;
        public string TimeZoneId { get; init; } = "Europe/Budapest";
        public string[] RecipientAddressesTemplate { get; init; } = [];
        public string SubjectTemplate {  get; init; } = "Asset Management - Nem leadott eszközök - Áruház {0}";
        public bool SendEmptyReports { get; init; }
        public bool TestMode { get; init; }
        public string[] TestRecipientAddressesTemplate {  get; init; } = [];
    }
}
