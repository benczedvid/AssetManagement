/**
 * Represents a request to dispose of an asset.
 */
export interface DisposeAssetRequest {
  /**
   * The mandatory disposal reason.
   * Must not exceed 2000 characters after trimming.
   */
  readonly reason: string;
}