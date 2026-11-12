namespace AssetManagement.Domain.Entities.Assets.Exceptions;

public sealed class AssetStateConflictException : InvalidOperationException
{
    public AssetStateConflictException(string message) : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }
}