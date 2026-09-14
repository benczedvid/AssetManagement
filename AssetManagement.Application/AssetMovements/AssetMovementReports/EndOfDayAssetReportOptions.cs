namespace AssetManagement.Application.AssetMovements.AssetMovementReports
{
    public sealed class EndOfDayAssetReportOptions
    {
        public const string SectionName = "EndOfDayAssetReport";
        public bool Enabled { get; init; }
        public string CronExpression { get; init; } = string.Empty;
        public string TimeZoneId { get; init; } = "Europe/Budapest";
        public string RecipientAddressTemplate { get; init; } = "HUST{0}SM@praktiker.hu";
        public string SubjectTemplate {  get; init; } = "Asset Management - Nem leadott eszközök - Áruház {0}";
        public bool SendEmptyReports { get; init; }
        public bool TestMode { get; init; }
        public string TestRecipientAddress {  get; init; } = string.Empty;
    }
}
