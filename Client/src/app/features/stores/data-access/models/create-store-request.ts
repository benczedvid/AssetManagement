import { Address } from "../../../../shared/models/address-model";

export interface CreateStoreRequest {
  readonly storeNumber: string;
  readonly name: string;
  readonly address: Address;
}
