import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ReconcileServiceDeskRequest } from '../models/reconcile-service-desk-request';
import { ReconcileServiceDeskResponse } from '../models/reconcile-service-desk-response';
import { GetServiceDeskSyncOperationsRequest } from '../models/get-service-desk-sync-operations-request';
import { GetServiceDeskSyncOperationsResponse } from '../models/get-service-desk-sync-operations-response';
import { ResolveServiceDeskReconciliationRequest } from '../models/resolve-service-desk-reconciliation-request';
import { ResolveServiceDeskReconciliationResponse } from '../models/resolve-service-desk-reconciliation-response';

// ServiceDesk synchronization status.
export { ServiceDeskSyncStatus } from '../models/service-desk-sync-status';
export type { ServiceDeskSyncStatusValue } from '../models/service-desk-sync-status';

// Request reconciliation.
export type { ReconcileServiceDeskRequest } from '../models/reconcile-service-desk-request';
export type { ReconcileServiceDeskResponse } from '../models/reconcile-service-desk-response';

// Synchronization operations.
export type { GetServiceDeskSyncOperationsRequest } from '../models/get-service-desk-sync-operations-request';
export type { GetServiceDeskSyncOperationsResponse } from '../models/get-service-desk-sync-operations-response';

// Administrative reconciliation.
export type { ResolveServiceDeskReconciliationRequest } from '../models/resolve-service-desk-reconciliation-request';
export type { ResolveServiceDeskReconciliationResponse } from '../models/resolve-service-desk-reconciliation-response';

@Injectable({
  providedIn: 'root',
})
export class ServiceDeskApi {
  private readonly http = inject(HttpClient);

  private readonly baseUrl = `${environment.api.baseUrl}/service-desk`;

  getOperations(request: GetServiceDeskSyncOperationsRequest = {}): Observable<GetServiceDeskSyncOperationsResponse> {
    let params = new HttpParams();

    if (request.status) {
      params = params.set('status', request.status);
    }

    if (request.operationType) {
      params = params.set('operationType', request.operationType);
    }

    if (request.serviceCaseId) {
      params = params.set('serviceCaseId', request.serviceCaseId);
    }

    return this.http.get<GetServiceDeskSyncOperationsResponse>(`${this.baseUrl}/operations`, { params });
  }

  reconcileRequest(serviceCaseId: string, request: ReconcileServiceDeskRequest): Observable<ReconcileServiceDeskResponse> {
    return this.http.post<ReconcileServiceDeskResponse>(`${this.baseUrl}/requests/${encodeURIComponent(serviceCaseId)}/reconcile`, request);
  }

  resolveReconciliation(request: ResolveServiceDeskReconciliationRequest): Observable<ResolveServiceDeskReconciliationResponse> {
    return this.http.post<ResolveServiceDeskReconciliationResponse>(`${this.baseUrl}/reconciliation/resolve`, request);
  }
}