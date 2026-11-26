import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PackageReceive } from './package-receive';

describe('PackageReceive', () => {
  let component: PackageReceive;
  let fixture: ComponentFixture<PackageReceive>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PackageReceive]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PackageReceive);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
