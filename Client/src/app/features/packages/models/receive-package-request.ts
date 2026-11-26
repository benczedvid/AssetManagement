export interface ReceivePackageRequest {
  readonly receivingStoreId: string;
  readonly comment: string | null;
}