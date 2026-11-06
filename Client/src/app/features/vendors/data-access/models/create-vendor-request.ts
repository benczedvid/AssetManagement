import { Address, } from "../../../../shared/models/address-model";

export interface CreateVendorRequest {
    readonly name: string;
    readonly address: Address;    readonly webpage: string | null;
}