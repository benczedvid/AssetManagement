import type { ServiceCaseListItem } from './service-case-list-item';

export interface GetServiceCasesResponse {
  readonly items: readonly ServiceCaseListItem[];
}