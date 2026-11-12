namespace AssetManagement.Infrastructure.ServiceDesk;

/// <summary>
/// Indicates that request creation failed before the client
/// invoked the HTTP operation that creates the external request.
/// </summary>
public sealed class ServiceDeskRequestNotSentException : Exception
{
    public ServiceDeskRequestNotSentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}