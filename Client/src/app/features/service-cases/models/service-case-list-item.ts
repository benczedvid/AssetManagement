import type { ServiceCaseStatusValue } from './service-case-status';
import type { ServiceIssueTypeValue } from './service-issue-type';

export interface ServiceCaseListItem {
  readonly id: string;
  readonly caseNumber: string;
  readonly assetId: string;
  readonly status: ServiceCaseStatusValue;
  readonly issueType: ServiceIssueTypeValue;
  readonly originStoreId: string | null;
  readonly vendorId: string | null;
  readonly isActive: boolean;
  readonly createdAtUtc: string;
  readonly createdByUserName: string;
  readonly closedAtUtc: string | null;
}