import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { VendorApiService } from './vendor-api';

describe('VendorApiService', () => {
  let service: VendorApiService;
  let httpTesting: HttpTestingController;
  const baseUrl = `${environment.api.baseUrl}/vendors`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(VendorApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('attaches a contact with PUT', () => {
    service.attachContact('v1', 'c1').subscribe();

    const request = httpTesting.expectOne(`${baseUrl}/v1/contacts/c1`);
    expect(request.request.method).toBe('PUT');
    request.flush(null);
  });

  it('detaches a contact with DELETE', () => {
    service.detachContact('v1', 'c1').subscribe();

    const request = httpTesting.expectOne(`${baseUrl}/v1/contacts/c1`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
