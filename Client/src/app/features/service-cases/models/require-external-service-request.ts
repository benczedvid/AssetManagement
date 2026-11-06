import type { ServiceCaseCommentRequest } from './service-case-comment-request';

export interface RequireExternalServiceRequest
  extends ServiceCaseCommentRequest {
  readonly vendorId: string;
}