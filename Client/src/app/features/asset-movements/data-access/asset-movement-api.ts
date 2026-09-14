import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { CreateAssetMovementRequest } from './models/create-asset-movement-request';
import { Observable } from 'rxjs';
import { CreateAssetMovementResponse } from './models/create-asset-movement-response';

@Injectable({
  providedIn: 'root',
})
export class AssetMovementApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/asset-movement`

  create(request: CreateAssetMovementRequest): Observable<CreateAssetMovementResponse>{
    return this.http.post<CreateAssetMovementResponse>(`${this.baseUrl}`, request)
  }
}
