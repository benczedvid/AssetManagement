import { Address } from "../../../../shared/models/address-model";
import { ContactResponse } from "../../../../shared/models/contacts-response";


export interface UpdateVendorResponse {
  readonly id: string;
  readonly name: string;
  readonly address: Address;
  readonly webpage: string | null;
  readonly contacts: readonly ContactResponse[];
}