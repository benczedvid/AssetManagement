import { TestBed } from '@angular/core/testing';

import { VendorApi } from './vendor-api';

describe('VendorApi', () => {
  let service: VendorApi;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(VendorApi);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
