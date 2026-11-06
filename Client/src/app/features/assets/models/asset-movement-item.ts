import { AssetMovementType } from '../../asset-movements/models/asset-movement-type';

export interface AssetMovementItem {
  readonly id: string;
  readonly type: AssetMovementType;
  readonly employeeNumber: string;
  readonly employeeName: string;
  readonly storeId: string;
  readonly storeName: string;
  readonly createdAtUtc: string;
}
