using AssetManagement.Domain.Entities.AssetMovements;

namespace AssetManagement.Application.AssetMovements.CreateAssetMovement
{
    public sealed record CreateAssetMovementResponse
    (
        Guid Id,
        Guid AssetId,
        Guid StoreId,
        string SerialNumber,
        string EmployeeNumber,
        AssetMovementType Type,
        DateTime CreatedAtUtc
        );
}
