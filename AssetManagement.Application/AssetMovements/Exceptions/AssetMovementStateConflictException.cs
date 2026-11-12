namespace AssetManagement.Application.AssetMovements.Exceptions;

public sealed class AssetMovementStateConflictException : Exception
{
    public AssetMovementStateConflictException(string message) : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }
}