import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PackageAssignCourier } from './package-assign-courier';

describe('PackageAssignCourier', () => {
  let component: PackageAssignCourier;
  let fixture: ComponentFixture<PackageAssignCourier>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PackageAssignCourier]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PackageAssignCourier);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
