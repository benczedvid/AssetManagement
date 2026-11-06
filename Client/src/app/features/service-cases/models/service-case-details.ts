import type { ServiceCaseAction } from './service-case-action';
import type { ServiceCaseEvent } from './service-case-event';
import type { ServiceCaseListItem } from './service-case-list-item';

export interface ServiceCaseDetails extends ServiceCaseListItem {
  readonly otherIssueDescription: string | null;
  readonly description: string;
  readonly createdByUserId: string;
  readonly closedByUserId: string | null;
  readonly events: readonly ServiceCaseEvent[];
  readonly availableActions: readonly ServiceCaseAction[];
}

export function hasServiceCaseAction(serviceCase: ServiceCaseDetails, action: ServiceCaseAction): boolean {
  return serviceCase.availableActions.includes(action);
}