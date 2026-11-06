import { Address } from "../../../../shared/models/address-model";

export interface GetVendorsResponse {
    readonly id: string;
    readonly name: string;
    readonly address: Address;
    readonly webpage: string | null;
}