import { AssetMovementType } from '../../../asset-movements/data-access/models/asset-movement-type';

export interface AssetHistoryItem {
  readonly id: string;
  readonly type: AssetMovementType;
  readonly employeeNumber: string;
  readonly employeeName: string;
  readonly storeId: string;
  readonly storeName: string;
  readonly createdAtUtc: string;
}
