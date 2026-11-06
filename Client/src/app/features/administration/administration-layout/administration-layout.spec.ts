import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdministrationLayout } from './administration-layout';

describe('AdministrationLayout', () => {
  let component: AdministrationLayout;
  let fixture: ComponentFixture<AdministrationLayout>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdministrationLayout]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdministrationLayout);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
