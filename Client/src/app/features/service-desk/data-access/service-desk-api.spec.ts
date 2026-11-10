import { TestBed } from '@angular/core/testing';

import { ServiceDeskApi } from './service-desk-api';

describe('ServiceDeskApi', () => {
  let service: ServiceDeskApi;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ServiceDeskApi);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
