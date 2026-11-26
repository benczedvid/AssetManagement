import { PackageEvent } from './package-event';
import { PackageItem } from './package-item';
import { PackageStatus } from './package-status';

export interface PackageDetails {
  readonly id: string;
  readonly packageNumber: string;
  readonly status: PackageStatus;
  readonly sourceStoreId: string;
  readonly sourceStoreNumber: string;
  readonly sourceStoreName: string;
  readonly destinationStoreId: string;
  readonly destinationStoreNumber: string;
  readonly destinationStoreName: string;
  readonly courierUserId: string | null;
  readonly courierUserName: string | null;
  readonly comment: string | null;
  readonly createdAtUtc: string;
  readonly createdByUserId: string;
  readonly dispatchedAtUtc: string | null;
  readonly dispatchedByUserId: string | null;
  readonly receivedAtUtc: string | null;
  readonly receivedByUserId: string | null;
  readonly cancelledAtUtc: string | null;
  readonly cancelledByUserId: string | null;
  readonly items: readonly PackageItem[];
  readonly events: readonly PackageEvent[];
}
