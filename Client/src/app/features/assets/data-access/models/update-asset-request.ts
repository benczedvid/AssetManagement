import { AssetStatus } from "./asset-status";
import { AssetType } from "./asset-type";

export interface UpdateAssetRequest {
  readonly assetName: string | null;
  readonly assetStatus: AssetStatus;
  readonly assetType: AssetType;
  readonly manufacturer: string | null;
  readonly model: string | null;
  readonly serialNumber: string;
  readonly macAddress: string | null;
  readonly wifiMacAddress: string | null;
  readonly imei: string | null;
  readonly assignedStoreId: string | null;
  readonly operatingSystem: string | null;
  readonly operatingSystemVersion: string | null;
  readonly assignedVendorId: string | null;
  readonly rfidTagId: string | null;
}
