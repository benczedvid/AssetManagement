namespace AssetManagement.Application.AssetMovements.AssetMovementReports
{
    public sealed record EndOfDayAssetReportResult
    (
        int StoreCount,
        int SentEmailCount,
        int SkippedStoreCount,
        int UnreturnedAssetCount,
        DateTime GeneratedAtUtc
        );
}
