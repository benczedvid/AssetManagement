namespace AssetManagement.Application.ServiceCases.Exceptions;

public sealed class ServiceCaseAccessDeniedException : Exception
{
    public ServiceCaseAccessDeniedException() : base("The current user is not authorized to access this service case or perform the requested operation.")
    {
    }

    public ServiceCaseAccessDeniedException(string message) : base(message)
    {
    }
}