import { TestBed } from '@angular/core/testing';
import { AssetMovement } from './asset-movement-api'


describe('AssetMovement', () => {
  let service: AssetMovement;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AssetMovement);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
