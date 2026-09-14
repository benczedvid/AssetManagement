import { TestBed } from '@angular/core/testing';

import { AssetApi } from './asset-api';

describe('AssetApi', () => {
  let service: AssetApi;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AssetApi);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
