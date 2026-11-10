namespace AssetManagement.Domain.Entities.ServiceDesk;

/// <summary>
/// Represents the synchronization state of a ServiceDesk request note.
/// </summary>
public enum ServiceDeskNoteOutboxStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    RequiresReconciliation = 5
}