namespace AssetManagement.Domain.Entities.ServiceDesk;

public enum ServiceDeskRequestOutboxStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    RequiresReconciliation = 4
}