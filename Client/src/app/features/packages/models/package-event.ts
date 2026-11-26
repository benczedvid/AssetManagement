import { PackageEventTypeValue } from './package-event-type';
import { PackageStatus } from './package-status';

export interface PackageEvent {
  readonly id: string;
  readonly type: PackageEventTypeValue;
  readonly previousStatus: PackageStatus | null;
  readonly newStatus: PackageStatus;
  readonly relatedAssetId: string | null;
  readonly relatedUserId: string | null;
  readonly comment: string | null;
  readonly performedByUserId: string;
  readonly performedByUserName: string;
  readonly occurredAtUtc: string;
}