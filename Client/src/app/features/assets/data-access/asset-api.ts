import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { GetAssetsResponse } from '../models/get-assets-response';
import { CreateAssetRequest } from '../models/create-asset-request';
import { AssetDetailsModel } from '../models/asset-details-model';
import { UpdateAssetRequest } from '../models/update-asset-request';
import { AssetHistoryModel } from '../models/asset-history-model';
import { DisposeAssetRequest } from '../models/dispose-asset-request';
import { DisposeAssetResponse } from '../models/dispose-asset-response';

/**
 * Provides HTTP operations for assets and their history.
 *
 * Authorization and business rules are enforced by the backend.
 */
@Injectable({
  providedIn: 'root',
})
export class AssetApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.api.baseUrl}/assets`;

  /**
   * Retrieves the assets accessible to the current user.
   *
   * @returns An observable containing the asset list.
   */
  getAll(): Observable<readonly GetAssetsResponse[]> {
    return this.http.get<readonly GetAssetsResponse[]>(this.baseUrl);
  }

  /**
   * Creates an asset.
   *
   * @param request The asset creation data.
   * @returns An observable containing the server response.
   */
  create(request: CreateAssetRequest): Observable<unknown> {
    return this.http.post<unknown>(this.baseUrl, request);
  }

  /**
   * Retrieves detailed information about an asset.
   *
   * @param assetId The unique identifier of the asset.
   * @returns An observable containing the asset details.
   */
  getDetails(assetId: string): Observable<AssetDetailsModel> {
    return this.http.get<AssetDetailsModel>(
      `${this.baseUrl}/${assetId}/details`,
    );
  }

  /**
   * Updates an existing asset.
   *
   * @param assetId The unique identifier of the asset.
   * @param request The asset update data.
   * @returns An observable containing the server response.
   */
  update(assetId: string, request: UpdateAssetRequest): Observable<unknown> {
    return this.http.put<unknown>(`${this.baseUrl}/${assetId}`, request);
  }

  /**
   * Retrieves the modification history of an asset.
   *
   * @param assetId The unique identifier of the asset.
   * @returns An observable containing the asset history entries.
   */
  getAssetHistory(assetId: string): Observable<AssetHistoryModel[]> {
    return this.http.get<AssetHistoryModel[]>(`${this.baseUrl}/${assetId}/history`);
  }

  /**
   * Disposes of an asset through the standalone disposal endpoint.
   *
   * Only active application administrators may perform this operation.
   * Assets with an active service case must be disposed of through
   * the service case workflow instead.
   *
   * The backend validates the reason, asset status, user assignments,
   * and authorization, and records the disposal history.
   *
   * This method does not automatically retry the request.
   *
   * @param assetId The unique identifier of the asset to dispose of.
   * @param request The request containing the mandatory disposal reason.
   * @returns An observable containing the disposal result.
   */
  dispose(assetId: string, request: DisposeAssetRequest): Observable<DisposeAssetResponse> {
    return this.http.post<DisposeAssetResponse>(`${this.baseUrl}/${assetId}/dispose`, request);
  }
}