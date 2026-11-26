import { AssetStatus } from '../../assets/models/asset-status';
import { PackageStatus } from './package-status';

export interface ReceivePackageAssetResponse {
  readonly assetId: string;
  readonly assignedStoreId: string;
  readonly previousStatus: AssetStatus;
  readonly newStatus: AssetStatus;
}

export interface ReceivePackageResponse {
  readonly packageId: string;
  readonly packageNumber: string;
  readonly packageStatus: PackageStatus;
  readonly destinationStoreId: string;
  readonly assets: readonly ReceivePackageAssetResponse[];
  readonly receivedAtUtc: string;
  readonly receivedByUserId: string;
  readonly receivedByUserName: string;
}