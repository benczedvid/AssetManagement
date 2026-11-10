namespace AssetManagement.Domain.Entities.ServiceCases;

public enum ServiceDeskSyncStatus
{
    NotStarted = 0,
    Pending = 1,
    Completed = 2,
    Failed = 3,
    RequiresReconciliation = 4
}