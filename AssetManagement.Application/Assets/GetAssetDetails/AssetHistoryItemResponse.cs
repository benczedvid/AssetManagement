using AssetManagement.Domain.Entities.AssetMovements;

namespace AssetManagement.Application.Assets.GetAssetDetails
{
    public sealed record AssetHistoryItemResponse
    (
        Guid Id,
        AssetMovementType Type,
        string EmployeeNumber,
        string EmployeeName,
        Guid StoreId,
        string StoreName,
        DateTime CreatedAtUtc
        );
}
