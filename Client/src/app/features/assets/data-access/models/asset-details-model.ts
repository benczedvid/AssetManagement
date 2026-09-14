import { AssetType } from './asset-type';
import { AssetStatus } from './asset-status';
import { AssetHistoryItem } from './asset-history-item';

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
  readonly history: readonly AssetHistoryItem[];
  readonly createdAtUtc: string;
  readonly assignedVendorId: string | null;
  readonly rfidTagId: string | null;
}
