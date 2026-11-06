namespace AssetManagement.Application.ServiceCases.Exceptions;

public sealed class ServiceCaseNotFoundException : Exception
{
    public Guid ServiceCaseId { get; }

    public ServiceCaseNotFoundException(Guid serviceCaseId) : base($"The service case with identifier '{serviceCaseId}' could not be found.")
    {
        ServiceCaseId = serviceCaseId;
    }
}