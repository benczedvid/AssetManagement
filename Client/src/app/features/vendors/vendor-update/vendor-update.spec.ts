import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VendorUpdate } from './vendor-update';

describe('VendorUpdate', () => {
  let component: VendorUpdate;
  let fixture: ComponentFixture<VendorUpdate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VendorUpdate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VendorUpdate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
