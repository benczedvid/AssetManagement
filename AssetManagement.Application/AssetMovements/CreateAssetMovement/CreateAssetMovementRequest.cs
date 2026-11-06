namespace AssetManagement.Application.AssetMovements.CreateAssetMovement
{
    /// <summary>
    /// Represents a request to create an asset movement.
    /// </summary>
    /// <param name="RfidTagId">
    /// The RFID tag identifier of the asset involved in the movement.
    /// </param>
    /// <param name="EmployeeNumber">
    /// The employee number identifying the employee involved in the movement.
    /// </param>
    public sealed record CreateAssetMovementRequest
    (
        string RfidTagId,
        string EmployeeNumber
        );
}
