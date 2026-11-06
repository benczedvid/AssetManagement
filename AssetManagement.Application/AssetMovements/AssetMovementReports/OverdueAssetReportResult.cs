namespace AssetManagement.Application.AssetMovements.AssetMovementReports;

public sealed record OverdueAssetReportResult(
    int StoreCount,
    int OverdueAssetCount,
    bool EmailSent,
    DateTime GeneratedAtUtc
);