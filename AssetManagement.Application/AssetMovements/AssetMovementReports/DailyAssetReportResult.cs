namespace AssetManagement.Application.AssetMovements.AssetMovementReports
{
    public sealed record DailyAssetReportResult
    (
        int StoreCount,
        int SentEmailCount,
        int SkippedStoreCount,
        int UnreturnedAssetCount,
        DateTime GeneratedAtUtc
        );
}
