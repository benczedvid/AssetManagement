export type ServiceDeskSyncOperationType =
  | 'RequestCreation'
  | 'NoteCreation'
  | 'RequestClosure';

export type ServiceDeskSyncOperationStatus =
  | 'Pending'
  | 'Processing'
  | 'Completed'
  | 'Failed'
  | 'RequiresReconciliation';

export interface ServiceDeskSyncOperationItem {
  outboxId: string;
  serviceCaseId: string;
  serviceCaseEventId: string;

  operationType: ServiceDeskSyncOperationType;
  status: ServiceDeskSyncOperationStatus;

  processingOperationId: string | null;
  externalIdentifier: string | null;

  createdAtUtc: string;
  processingStartedAtUtc: string | null;
  processedAtUtc: string | null;

  error: string | null;

  readonly serviceCaseNumber: string;
}

export interface GetServiceDeskSyncOperationsResponse {
  items: ServiceDeskSyncOperationItem[];
}