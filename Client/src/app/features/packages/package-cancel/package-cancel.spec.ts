import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PackageCancel } from './package-cancel';

describe('PackageCancel', () => {
  let component: PackageCancel;
  let fixture: ComponentFixture<PackageCancel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PackageCancel]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PackageCancel);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
