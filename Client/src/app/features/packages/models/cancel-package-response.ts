import { PackageStatus } from './package-status';

export interface CancelPackageResponse {
  readonly packageId: string;
  readonly packageNumber: string;
  readonly packageStatus: PackageStatus;
  readonly cancelledAtUtc: string;
  readonly cancelledByUserId: string;
  readonly cancelledByUserName: string;
  readonly comment: string;
}