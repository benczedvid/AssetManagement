namespace AssetManagement.Domain.Entities.ServiceCases
{
    public enum ServiceCaseStatus
    {
        Unknown = 0,

        AwaitingCentralReceipt = 1, // The service case has been created and is waiting to be received by the central service team.
        UnderCentralInspection = 2, // The service case is being inspected by the central service team to determine the issue and the next steps.
        AwaitingExternalServiceDispatch = 3, // The service case has been inspected and is waiting to be sent to an external service provider for repair or further inspection.
        SentToExternalService = 4, // The service case has been sent to an external service provider for repair or further inspection.
        InExternalService = 5, // The service case is currently being serviced by an external provider.
        AwaitingReturn = 6, // The service case is waiting to be returned to the store.
        ReturningToStore = 7, // The service case is being returned to the store.
        Resolved = 8, // The service case has been resolved.
        Closed = 9, // The service case has been closed.
        Cancelled = 10
    }
}