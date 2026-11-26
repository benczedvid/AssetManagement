export interface CreatePackageRequest {
  readonly sourceStoreId: string;
  readonly destinationStoreId: string;
  readonly assetIds: readonly string[];
  readonly courierUserId: string | null;
  readonly comment: string | null;
}