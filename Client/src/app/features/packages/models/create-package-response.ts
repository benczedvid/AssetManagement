import { PackageStatus } from './package-status';

export interface CreatePackageResponse {
  readonly id: string;
  readonly packageNumber: string;
  readonly status: PackageStatus;
  readonly sourceStoreId: string;
  readonly destinationStoreId: string;
  readonly courierUserId: string | null;
  readonly assetIds: readonly string[];
  readonly createdAtUtc: string;
}