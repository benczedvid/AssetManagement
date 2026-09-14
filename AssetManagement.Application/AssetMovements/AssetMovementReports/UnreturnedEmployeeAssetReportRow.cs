namespace AssetManagement.Application.AssetMovements.AssetMovementReports
{
    public sealed record UnreturnedEmployeeAssetReportRow(
        Guid AssetMovementId,
        Guid AssetId,
        string AssetName,
        string AssetSerialNumber,
        string EmployeeNumber,
        string EmployeeFullName,
        Guid StoreId,
        string StoreNumber,
        string StoreName,
        DateTime CheckedOutAtUtc
    );
}
