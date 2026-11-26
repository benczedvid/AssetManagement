import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { GetVendorByIdResponse } from './models/get-vendor-by-id-response';
import { GetVendorsResponse } from './models/get-vendors-response';
import { SaveVendorRequest } from './models/save-vendor-request';

@Injectable({
  providedIn: 'root',
})
export class VendorApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.api.baseUrl}/vendors`;

  getAll(): Observable<readonly GetVendorsResponse[]> {
    return this.http.get<readonly GetVendorsResponse[]>(this.baseUrl);
  }

  getById(vendorId: string): Observable<GetVendorByIdResponse> {
    return this.http.get<GetVendorByIdResponse>(`${this.baseUrl}/${vendorId}`);
  }

  create(request: SaveVendorRequest): Observable<GetVendorsResponse> {
    return this.http.post<GetVendorsResponse>(this.baseUrl, request);
  }

  update(vendorId: string, request: SaveVendorRequest): Observable<GetVendorByIdResponse> {
    return this.http.put<GetVendorByIdResponse>(`${this.baseUrl}/${vendorId}`, request);
  }

  attachContact(vendorId: string, contactId: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${vendorId}/contacts/${contactId}`, null);
  }

  detachContact(vendorId: string, contactId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${vendorId}/contacts/${contactId}`);
  }
}
