export const ServiceDeskSyncStatus = {
  NotStarted: 'NotStarted',
  Pending: 'Pending',
  Completed: 'Completed',
  Failed: 'Failed',
  RequiresReconciliation: 'RequiresReconciliation',
} as const;

export type ServiceDeskSyncStatusValue =
  (typeof ServiceDeskSyncStatus)[keyof typeof ServiceDeskSyncStatus];