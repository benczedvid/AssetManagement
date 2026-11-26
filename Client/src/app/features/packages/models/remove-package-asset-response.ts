import type { PackageStatus } from './package-status';

export interface RemovePackageAssetResponse {
  readonly packageId: string;
  readonly packageNumber: string;
  readonly status: PackageStatus;
  readonly assetId: string;
  readonly assetCount: number;
  readonly removedAtUtc: string;
}