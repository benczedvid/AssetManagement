import { TestBed } from '@angular/core/testing';

import { ApplicationUsersApi } from './application-users-api';

describe('ApplicationUsersApi', () => {
  let service: ApplicationUsersApi;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ApplicationUsersApi);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
