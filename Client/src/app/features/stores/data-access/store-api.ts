import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { Observable } from 'rxjs';
import { GetStoresResponse } from './models/get-stores-response';
import { CreateStoreRequest } from './models/create-store-request';

@Injectable({
  providedIn: 'root',
})
export class StoreApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.api.baseUrl}/stores`;

  getAll(): Observable<readonly GetStoresResponse[]> {
    return this.http.get<readonly GetStoresResponse[]>(`${this.baseUrl}`);
  }

  create(request: CreateStoreRequest): Observable<unknown> {
    return this.http.post<unknown>(`${this.baseUrl}`, request);
  }
}
