import { PackageStatus } from './package-status';

export interface AssignCourierResponse {
  readonly packageId: string;
  readonly packageNumber: string;
  readonly packageStatus: PackageStatus;
  readonly courierUserId: string | null;
  readonly updatedAtUtc: string;
}