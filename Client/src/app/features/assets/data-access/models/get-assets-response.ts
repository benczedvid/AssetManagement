import { AssetStatus } from "./asset-status";
import { AssetType } from "./asset-type";

export interface GetAssetsResponse {
  readonly assetId: string;
  readonly assetName: string;
  readonly assetType: AssetType;
  readonly assetStatus: AssetStatus;
  readonly manufacturer: string;
  readonly model: string;
  readonly serialNumber: string;
  readonly createdAtUtc: string;
  readonly macAddress: string | null;
  readonly wifiMacAddress: string | null;
  readonly imei: string | null;
  readonly operatingSystem: string | null;
  readonly operatingSystemVersion: string | null;
  readonly assignedStoreId: string;
  readonly assignedUserId: string | null;
  readonly assignedEmployeeName: string | null;
  readonly assignedVendorId: string | null;
  readonly assignedVendorName: string | null;
  readonly rfidTagId: string | null;
}
