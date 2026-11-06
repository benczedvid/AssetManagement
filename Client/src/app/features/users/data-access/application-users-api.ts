import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

import { ApplicationUserListItem } from '../models/application-user-list-item';

@Injectable({
  providedIn: 'root',
})
export class ApplicationUsersApi {
  private readonly http = inject(HttpClient);

  private readonly baseUrl = `${environment.api.baseUrl}/users`;

  getList(): Observable<ApplicationUserListItem[]> {
    return this.http.get<ApplicationUserListItem[]>(
      this.baseUrl,
    );
  }
}