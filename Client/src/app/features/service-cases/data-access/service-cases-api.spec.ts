import { TestBed } from '@angular/core/testing';

import { ServiceCasesApi } from './service-cases-api';

describe('ServiceCasesApi', () => {
  let service: ServiceCasesApi;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ServiceCasesApi);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
