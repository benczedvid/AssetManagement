import { AssetType } from './asset-type';
import { AssetStatus } from './asset-status';
import { AssetMovementItem } from './asset-movement-item';

export interface AssetDetailsModel {
  readonly assetId: string;
  readonly assetName: string;
  readonly assetType: AssetType;
  readonly assetStatus: AssetStatus;
  readonly manufacturer: string;
  readonly model: string;
  readonly serialNumber: string;
  readonly macAddress: string | null;
  readonly wifiMacAddress: string | null;
  readonly imei: string | null;
  readonly operatingSystem: string | null;
  readonly operatingSystemVersion: string | null;
  readonly assignedStoreId: string;
  readonly assignedStoreName: string;
  readonly assignedEmployeeNumber: string | null;
  readonly assignedEmployeeName: string | null;
  readonly history: readonly AssetMovementItem[];
  readonly createdAtUtc: string;
  readonly assignedVendorId: string | null;
  readonly assignedVendorName: string | null;
  readonly rfidTagId: string | null;
  readonly warrantyEndDate: string | null;
}