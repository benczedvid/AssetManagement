import type { ServiceCaseEventTypeValue } from './service-case-event-type';
import type { ServiceCaseStatusValue } from './service-case-status';

export interface ServiceCaseEvent {
  readonly id: string;
  readonly type: ServiceCaseEventTypeValue;
  readonly previousStatus: ServiceCaseStatusValue | null;
  readonly newStatus: ServiceCaseStatusValue;
  readonly comment: string | null;
  readonly performedByUserId: string;
  readonly performedByUserName: string;
  readonly occurredAtUtc: string;
}