namespace AssetManagement.Application.AssetMovements.CreateAssetMovement
{
    public sealed record CreateAssetMovementRequest
    (
        string RfidTagId,
        string EmployeeNumber
        );
}
