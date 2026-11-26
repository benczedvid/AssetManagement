import type { PackageStatus } from './package-status';

export interface AddPackageAssetResponse {
  readonly packageId: string;
  readonly packageNumber: string;
  readonly status: PackageStatus;
  readonly assetId: string;
  readonly assetCount: number;
  readonly addedAtUtc: string;
}