import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { StoreDashboardResponse } from './models/store-dashboard-response';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DashboardApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.api.baseUrl}/dashboard`;

  getStoreDashboard(): Observable<StoreDashboardResponse[]> {
    return this.http.get<StoreDashboardResponse[]>(
      `${this.baseUrl}/stores`
    );
  }
}
