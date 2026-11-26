import { Address } from '../../../../shared/models/address-model';

/** Request body used both for creating and for updating a vendor. */
export interface SaveVendorRequest {
  readonly name: string;
  readonly address: Address;
  readonly webpage: string | null;
}
