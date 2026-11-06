namespace AssetManagement.Application.ServiceCases.Exceptions;

public sealed class ServiceCaseAlreadyExistsException : Exception
{
    public Guid AssetId { get; }

    public ServiceCaseAlreadyExistsException(Guid assetId) : base($"The asset with identifier '{assetId}' already has an active service case.")
    {
        AssetId = assetId;
    }
}