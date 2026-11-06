/**
 * Represents a request to dispose of the asset associated
 * with a service case.
 */
export interface DisposeServiceCaseAssetRequest {
  /**
   * The mandatory disposal reason.
   * Must contain between 1 and 2000 characters after trimming.
   */
  readonly reason: string;
}