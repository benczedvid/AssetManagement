import { AssetStatus } from '../../assets/models/asset-status';
import { PackageStatus } from './package-status';

export interface DispatchPackageAssetResponse {
  readonly assetId: string;
  readonly previousStatus: AssetStatus;
  readonly newStatus: AssetStatus;
}

export interface DispatchPackageResponse {
  readonly packageId: string;
  readonly packageNumber: string;
  readonly packageStatus: PackageStatus;
  readonly sourceStoreId: string;
  readonly destinationStoreId: string;
  readonly courierUserId: string | null;
  readonly assets: readonly DispatchPackageAssetResponse[];
  readonly dispatchedAtUtc: string;
  readonly dispatchedByUserId: string;
  readonly dispatchedByUserName: string;
}