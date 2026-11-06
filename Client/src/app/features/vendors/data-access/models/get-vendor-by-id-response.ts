import { Address } from "../../../../shared/models/address-model";
import { ContactResponse } from "../../../../shared/models/contacts-response";

export interface GetVendorByIdResponse {
    readonly id: string;
    readonly name: string;
    readonly address: Address;
    readonly contacts: readonly ContactResponse[];
    readonly webpage: string | null;
}