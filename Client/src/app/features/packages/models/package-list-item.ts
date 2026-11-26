import type { PackageStatus } from './package-status';

/**
 * Represents an asset included in a package.
 */
export interface PackageListAsset {
  readonly assetId: string;
  readonly assetName: string;
  readonly serialNumber: string;
}

/**
 * Represents a package in the package list.
 */
export interface PackageListItem {
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

  readonly assetCount: number;

  /**
   * Assets included in the package.
   */

  readonly createdAtUtc: string;
  readonly createdByUserId: string;

  readonly dispatchedAtUtc: string | null;
  readonly receivedAtUtc: string | null;
}