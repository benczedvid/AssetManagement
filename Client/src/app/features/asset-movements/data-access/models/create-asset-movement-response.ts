import { AssetMovementType } from "./asset-movement-type";

export interface CreateAssetMovementResponse {
  readonly id: string;
  readonly assetId: string,
  readonly storeId: string,
  readonly serialNumber: string;
  readonly employeeNumber: string;
  readonly type: AssetMovementType;
  readonly createdAtUtc: string;
}
