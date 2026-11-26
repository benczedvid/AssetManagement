import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PackageCreate } from './package-create';

describe('PackageCreate', () => {
  let component: PackageCreate;
  let fixture: ComponentFixture<PackageCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PackageCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PackageCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
