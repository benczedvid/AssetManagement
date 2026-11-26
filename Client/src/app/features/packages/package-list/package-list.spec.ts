import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PackageList } from './package-list';

describe('PackageList', () => {
  let component: PackageList;
  let fixture: ComponentFixture<PackageList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PackageList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PackageList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
