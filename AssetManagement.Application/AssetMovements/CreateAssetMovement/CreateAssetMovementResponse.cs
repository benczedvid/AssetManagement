using AssetManagement.Domain.Entities.AssetMovements;

namespace AssetManagement.Application.AssetMovements.CreateAssetMovement
{
    /// <summary>
    /// Represents the result of a successfully created asset movement.
    /// </summary>
    /// <param name="Id">The unique identifier of the asset movement.</param>
    /// <param name="AssetId">The unique identifier of the asset involved in the movement.</param>
    /// <param name="StoreId">The unique identifier of the store associated with the asset movement.</param>
    /// <param name="SerialNumber">The serial number of the asset.</param>
    /// <param name="EmployeeNumber">The employee number identifying the employee involved in the movement.</param>
    /// <param name="Type">The type of the asset movement.</param>
    /// <param name="CreatedAtUtc">The date and time when the asset movement was created, in UTC.</param>
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
