import { ServiceDeskReconciliationOperation, ServiceDeskReconciliationOutcome } from './resolve-service-desk-reconciliation-request';

export interface ResolveServiceDeskReconciliationResponse {
  operation: ServiceDeskReconciliationOperation;

  outboxId: string;

  outcome: ServiceDeskReconciliationOutcome;

  verifiedAtUtc: string;
}