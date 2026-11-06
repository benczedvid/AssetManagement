import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AssetMovementCreate } from './asset-movement-create';

describe('AssetMovementCreate', () => {
  let component: AssetMovementCreate;
  let fixture: ComponentFixture<AssetMovementCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssetMovementCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AssetMovementCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
