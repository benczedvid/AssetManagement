import type { ServiceIssueTypeValue } from './service-issue-type';

export interface OpenServiceCaseRequest {
  readonly assetId: string;
  readonly issueType: ServiceIssueTypeValue;
  readonly otherIssueDescription: string | null;
  readonly description: string;
}