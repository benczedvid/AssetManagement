namespace AssetManagement.Domain.Entities.ServiceCases
{
    public enum ServiceCaseEventType
    {
        Unknown = 0,

        Created = 1, // The service case has been created.
        DispatchedToHeadquarters = 2, // The service case has been dispatched to the headquarters for inspection.
        InspectionStarted = 3, // The service case is being inspected by the headquarters to determine the issue and the next steps.
        RepairCompletedInHeadquarters = 4, // The service case has been repaired in the headquarters and is ready to be returned to the store.
        ExternalServiceRequired = 5, // The service case requires external service and has been sent to an external service provider for repair or further inspection.
        DispatchedToExternalService = 6, // The service case has been dispatched to an external service provider for repair or further inspection.
        ReceivedFromExternalService = 7, // The service case has been received from an external service provider after repair or further inspection.
        ReturnedFromExternalService = 8, // The service case has been returned from an external service provider after repair or further inspection.
        ReceivedInStore = 9, // The service case has been received in the store after repair or further inspection.
        Resolved = 10, // The service case has been resolved.
        Closed = 11, // The service case has been closed.
        Cancelled = 12, // The service case has been cancelled.
        ReceivedAtHeadquarters = 13, // The service case has been received at the headquarters for inspection.
        HandedOverToExternalService = 14, // The service case has been handed over to an external service provider for repair or further inspection.
        ReceivedByExternalService = 15, // The service case has been received by an external service provider for repair or further inspection.
        DispatchedToOriginStore = 16, // The service case has been dispatched back to the origin store after repair or further inspection.
        AssetDisposed = 17,
        PackageCreated = 18
    }
}