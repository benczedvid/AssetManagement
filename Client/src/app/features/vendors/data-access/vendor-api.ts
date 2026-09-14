import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { Observable } from 'rxjs';
import { GetVendorsResponse } from './models/get-vendors-response';
import { CreateVendorRequest } from './models/create-vendor-request';
import { UpdateVendorRequest } from './models/update-vendor-request';
import { GetVendorByIdResponse } from './models/get-vendor-by-id-response';
import { CreateVendorResponse } from './models/create-vendor-response';
import { UpdateVendorResponse } from './models/update-vendor-response';

@Injectable({
  providedIn: 'root',
})
export class VendorApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.api.baseUrl}/vendors`;

  getAll(): Observable<readonly GetVendorsResponse[]> {
    return this.http.get<readonly GetVendorsResponse[]>(`${this.baseUrl}`);
  }

  create(request: CreateVendorRequest): Observable<CreateVendorResponse> {
    return this.http.post<CreateVendorResponse>(`${this.baseUrl}`, request);
  }

  getById(vendorId: string): Observable<GetVendorByIdResponse> {
    return this.http.get<GetVendorByIdResponse>(`${this.baseUrl}/${vendorId}`);
  }

  update(vendorId: string, request: UpdateVendorRequest): Observable<UpdateVendorResponse> {
    return this.http.put<UpdateVendorResponse>(`${this.baseUrl}/${vendorId}`, request);
  }
}
