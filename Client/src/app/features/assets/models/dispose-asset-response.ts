import { AssetStatus } from './asset-status';

/**
 * Represents the result of successfully disposing of an asset.
 */
export interface DisposeAssetResponse {
  /**
   * The unique identifier of the disposed asset.
   */
  readonly assetId: string;

  /**
   * The name of the disposed asset.
   */
  readonly assetName: string;

  /**
   * The asset status after disposal.
   */
  readonly assetStatus: AssetStatus;

  /**
   * The validated and trimmed disposal reason.
   */
  readonly reason: string;

  /**
   * The server-generated disposal timestamp as a UTC ISO 8601 string.
   */
  readonly disposedAtUtc: string;

  /**
   * The internal application user identifier of the administrator
   * who performed the disposal.
   */
  readonly disposedByUserId: string;

  /**
   * The name of the administrator who performed the disposal.
   */
  readonly disposedByUserName: string;
}