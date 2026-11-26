import { TestBed } from '@angular/core/testing';

import { PackagesApi } from './packages-api';

describe('PackagesApi', () => {
  let service: PackagesApi;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PackagesApi);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
