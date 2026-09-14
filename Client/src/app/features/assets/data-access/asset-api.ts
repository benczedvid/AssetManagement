import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { GetAssetsResponse } from '../data-access/models/get-assets-response';
import { Observable } from 'rxjs';
import { CreateAssetRequest } from './models/create-asset-request';
import { AssetDetailsModel } from './models/asset-details-model';
import { UpdateAssetRequest } from './models/update-asset-request';

@Injectable({
  providedIn: 'root',
})
export class AssetApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.api.baseUrl}/assets`;

  getAll(): Observable<readonly GetAssetsResponse[]> {
    return this.http.get<readonly GetAssetsResponse[]>(`${this.baseUrl}`);
  }
  create(request: CreateAssetRequest): Observable<unknown> {
    return this.http.post<unknown>(`${this.baseUrl}`, request);
  }

  getDetails(assetId: string): Observable<AssetDetailsModel> {
    return this.http.get<AssetDetailsModel>(`${this.baseUrl}/${assetId}/details`)
  }
  update(assetId: string, request: UpdateAssetRequest): Observable<unknown> {
    return this.http.put<unknown>(`${this.baseUrl}/${assetId}`, request);
  }
}
