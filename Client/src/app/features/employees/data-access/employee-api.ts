import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetEmployeesResponse } from './models/get-employees-response';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EmployeeApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.api.baseUrl}/employees`;

  getAll(): Observable<GetEmployeesResponse[]> {
    return this.http.get<GetEmployeesResponse[]>(this.baseUrl);
  }

  getById(id: string): Observable<GetEmployeesResponse> {
    return this.http.get<GetEmployeesResponse>(
      `${this.baseUrl}/${id}`
    );
  }
}
