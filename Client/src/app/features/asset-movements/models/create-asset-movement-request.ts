import { AssetMovementType } from "./asset-movement-type";

export interface CreateAssetMovementRequest {
  readonly rfidTagId: string,
  readonly employeeNumber: string
}
