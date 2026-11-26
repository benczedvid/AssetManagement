import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PackageDispatch } from './package-dispatch';

describe('PackageDispatch', () => {
  let component: PackageDispatch;
  let fixture: ComponentFixture<PackageDispatch>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PackageDispatch]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PackageDispatch);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
