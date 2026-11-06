namespace AssetManagement.Application
    .AssetMovements
    .AssetMovementReports;

public sealed class OverdueAssetReportOptions
{
    public const string SectionName = "OverdueAssetReport";
    public bool Enabled { get; init; }
    public string CronExpression { get; init; } = string.Empty;
    public int MinimumUnreturnedDays { get; init; } = 3;
    public string TimeZoneId { get; init; } = "Europe/Budapest";
    public string[] RecipientAddresses { get; init; } = [];
    public string Subject { get; init; } = "Eszközkezelő - 3 napja nem leadott eszközök";
    public bool TestMode { get; init; }
    public string? TestRecipientAddress { get; init; }
}