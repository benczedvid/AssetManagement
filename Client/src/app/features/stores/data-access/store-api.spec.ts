import { TestBed } from '@angular/core/testing';

import { Stores } from './stores';

describe('Stores', () => {
  let service: Stores;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Stores);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
