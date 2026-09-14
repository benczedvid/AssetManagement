import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AssetUpdate } from './asset-update';

describe('AssetUpdate', () => {
  let component: AssetUpdate;
  let fixture: ComponentFixture<AssetUpdate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssetUpdate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AssetUpdate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
