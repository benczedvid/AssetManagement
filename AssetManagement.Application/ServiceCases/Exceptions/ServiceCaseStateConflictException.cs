namespace AssetManagement.Application.ServiceCases.Exceptions;

public sealed class ServiceCaseStateConflictException : Exception
{
    public Guid? ServiceCaseId { get; }

    public ServiceCaseStateConflictException(string message) : base(message)
    {
    }

    public ServiceCaseStateConflictException(Guid serviceCaseId, string message) : base(message)
    {
        ServiceCaseId = serviceCaseId;
    }
}