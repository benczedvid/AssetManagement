export interface ReconcileServiceDeskRequest {
  serviceCaseId: string;
  operationId: string;

  requestExists: boolean;
  serviceDeskRequestId: string | null;

  comment: string | null;
}