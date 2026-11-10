namespace AssetManagement.Domain.Entities.ServiceDesk;

public enum ServiceDeskRequestClosureOutboxStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    RequiresReconciliation = 4
}