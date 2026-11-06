import type { DisposeServiceCaseAssetRequest } from './dispose-service-case-asset-request';
import type { RequireExternalServiceRequest } from './require-external-service-request';
import type { ServiceCaseCommentRequest } from './service-case-comment-request';

/**
 * Maps each supported service case action
 * to its corresponding request payload.
 */
export interface ServiceCaseActionRequests {
  RequireExternalService: RequireExternalServiceRequest;
  DispatchToExternalService: ServiceCaseCommentRequest;
  HandOverToExternalService: ServiceCaseCommentRequest;
  ConfirmReceivedByExternalService: ServiceCaseCommentRequest;
  ReturnFromExternalService: ServiceCaseCommentRequest;
  CompleteRepairAtHeadquarters: ServiceCaseCommentRequest;
  ResolveAfterExternalServiceReturn: ServiceCaseCommentRequest;
  DisposeAsset: DisposeServiceCaseAssetRequest;
  Close: ServiceCaseCommentRequest;
}