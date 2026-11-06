import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, switchMap, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ServiceCaseAction } from '../models/service-case-action';
import { GetServiceCasesResponse } from '../models/get-service-cases-response';
import { ServiceCaseDetails } from '../models/service-case-details';
import { OpenServiceCaseRequest } from '../models/open-service-case-request';
import { ServiceCaseActionRequests } from '../models/service-case-action-requests';
import { ServiceCaseCommentRequest } from '../models/service-case-comment-request';
import { RequireExternalServiceRequest } from '../models/require-external-service-request';
import { DisposeServiceCaseAssetRequest } from '../models/dispose-service-case-asset-request';

export { ServiceCaseStatus } from '../models/service-case-status';
export type { ServiceCaseStatusValue } from '../models/service-case-status';
export { ServiceIssueType } from '../models/service-issue-type';
export type { ServiceIssueTypeValue } from '../models/service-issue-type';
export { SERVICE_CASE_ACTIONS } from '../models/service-case-action';
export type { ServiceCaseAction } from '../models/service-case-action';
export type { ServiceCaseListItem } from '../models/service-case-list-item';
export type { GetServiceCasesResponse } from '../models/get-service-cases-response';
export type { ServiceCaseEvent } from '../models/service-case-event';
export { hasServiceCaseAction } from '../models/service-case-details';
export type { ServiceCaseDetails } from '../models/service-case-details';
export type { OpenServiceCaseRequest } from '../models/open-service-case-request';
export type { ServiceCaseCommentRequest } from '../models/service-case-comment-request';
export type { RequireExternalServiceRequest } from '../models/require-external-service-request';
export type { ServiceCaseActionRequests } from '../models/service-case-action-requests';
export type { DisposeServiceCaseAssetRequest } from '../models/dispose-service-case-asset-request';

const ACTION_PATHS = {
  RequireExternalService: 'require-external-service',
  DispatchToExternalService: 'dispatch-to-external-service',
  HandOverToExternalService: 'hand-over-to-external-service',
  ConfirmReceivedByExternalService: 'confirm-received-by-external-service',
  ReturnFromExternalService: 'return-from-external-service',
  CompleteRepairAtHeadquarters: 'complete-repair-at-headquarters',
  ResolveAfterExternalServiceReturn: 'resolve-after-external-service-return',
  DisposeAsset: 'dispose-asset',
  Close: 'close',
} as const satisfies Partial<Record<ServiceCaseAction, string>>;

interface OpenServiceCaseResult {
  readonly id: string;
}

@Injectable({
  providedIn: 'root',
})
export class ServiceCasesApi {
  private readonly http = inject(HttpClient);

  private readonly baseUrl = `${environment.api.baseUrl}/service-cases`;

  getList(): Observable<GetServiceCasesResponse> {
    return this.http.get<GetServiceCasesResponse>(this.baseUrl);
  }

  getDetails(id: string): Observable<ServiceCaseDetails> {
    return this.http.get<ServiceCaseDetails>(
      this.getCaseUrl(id),
    );
  }

  open(request: OpenServiceCaseRequest): Observable<ServiceCaseDetails> {
    return this.http
      .post<OpenServiceCaseResult>(this.baseUrl, request)
      .pipe(switchMap(result => this.getDetails(result.id)),
      );
  }

executeAction<TAction extends keyof ServiceCaseActionRequests>(
  id: string,
  action: TAction,
  request: ServiceCaseActionRequests[TAction]
): Observable<ServiceCaseDetails> {

  const path = (
    ACTION_PATHS as Partial<Record<ServiceCaseAction, string>>
  )[action];

  if (!path) {
    return throwError(() => new Error(
      `No API route is configured for service case action: ${action}`
    ));
  }

  return this.http
    .post<unknown>(
      `${this.getCaseUrl(id)}/${path}`,
      request
    )
    .pipe(
      switchMap(() => this.getDetails(id))
    );
}

  requireExternalService(id: string, request: RequireExternalServiceRequest): Observable<ServiceCaseDetails> {
    return this.executeAction(id, 'RequireExternalService', request);
  }

  dispatchToExternalService(id: string, request: ServiceCaseCommentRequest = {}): Observable<ServiceCaseDetails> {
    return this.executeAction(id, 'DispatchToExternalService', request);
  }

  handOverToExternalService(id: string, request: ServiceCaseCommentRequest = {}): Observable<ServiceCaseDetails> {
    return this.executeAction(id, 'HandOverToExternalService', request);
  }

  confirmReceivedByExternalService(id: string, request: ServiceCaseCommentRequest = {}): Observable<ServiceCaseDetails> {
    return this.executeAction(id, 'ConfirmReceivedByExternalService', request);
  }

  returnFromExternalService(id: string, request: ServiceCaseCommentRequest = {}): Observable<ServiceCaseDetails> {
    return this.executeAction(id, 'ReturnFromExternalService', request,
    );
  }

  completeRepairAtHeadquarters(id: string, request: ServiceCaseCommentRequest = {}): Observable<ServiceCaseDetails> {
    return this.executeAction(id, 'CompleteRepairAtHeadquarters',  request);
  }

  resolveAfterExternalServiceReturn(id: string, request: ServiceCaseCommentRequest = {}): Observable<ServiceCaseDetails> {
    return this.executeAction(id, 'ResolveAfterExternalServiceReturn', request);
  }
    /**
   * Dispose of the asset associated with a se*vice case.
   *
   * The backend verifies that the current user is an active application
   * administrator and that the service case and asset states allow disposal.
   *
   * A successful operation changes the asset status to Disposed and the
   * service case status to Resolved. Closing the service case remains
   * a separate operation.
   *
   * @param id The unique identifier of the service case.
   * @param request The request containing the mandatory disposal reason.
   * @returns The refreshed service case details after disposal.
   */
  disposeAsset(id: string,  request: DisposeServiceCaseAssetRequest): Observable<ServiceCaseDetails> {
    return this.executeAction(id, 'DisposeAsset', request);
  }

  close(id: string, request: ServiceCaseCommentRequest = {}): Observable<ServiceCaseDetails> {
    return this.executeAction(id, 'Close', request);
  }

  private getCaseUrl(id: string): string {
    return `${this.baseUrl}/${encodeURIComponent(id)}`;
  }
}