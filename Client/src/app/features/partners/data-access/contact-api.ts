import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Contact, SaveContactRequest } from './models/contact';

@Injectable({
  providedIn: 'root',
})
export class ContactApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.api.baseUrl}/contacts`;

  getAll(): Observable<readonly Contact[]> {
    return this.http.get<readonly Contact[]>(this.baseUrl);
  }

  getById(contactId: string): Observable<Contact> {
    return this.http.get<Contact>(`${this.baseUrl}/${contactId}`);
  }

  create(request: SaveContactRequest): Observable<Contact> {
    return this.http.post<Contact>(this.baseUrl, request);
  }

  update(contactId: string, request: SaveContactRequest): Observable<Contact> {
    return this.http.put<Contact>(`${this.baseUrl}/${contactId}`, request);
  }
}
