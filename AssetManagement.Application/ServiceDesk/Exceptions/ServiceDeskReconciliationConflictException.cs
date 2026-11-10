namespace AssetManagement.Application.ServiceDesk.Exceptions;

/// <summary>
/// Represents a conflict during administrative
/// ServiceDesk reconciliation.
/// </summary>
public sealed class ServiceDeskReconciliationConflictException : Exception
{
    public ServiceDeskReconciliationConflictException(string message) : base(message)
    {
    }
}