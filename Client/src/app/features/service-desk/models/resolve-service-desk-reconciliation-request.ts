export enum ServiceDeskReconciliationOperation {
  RequestCreation = 1,
  RequestClosure = 2,
}

export enum ServiceDeskReconciliationOutcome {
  Completed = 1,
  Failed = 2,
}

export interface ResolveServiceDeskReconciliationRequest {
  operation: ServiceDeskReconciliationOperation;

  outboxId: string;
  processingOperationId: string;

  outcome: ServiceDeskReconciliationOutcome;

  serviceDeskRequestId: string | null;
  verificationComment: string;
}