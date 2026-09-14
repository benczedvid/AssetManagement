import { Address } from "../../../../shared/models/address-model";

export interface GetStoresResponse {
  readonly id: string;
  readonly storeNumber: string;
  readonly name: string;
  readonly address: Address;
}
