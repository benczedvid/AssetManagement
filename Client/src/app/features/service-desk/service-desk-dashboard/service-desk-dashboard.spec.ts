import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ServiceDeskDashboard } from './service-desk-dashboard';

describe('ServiceDeskDashboard', () => {
  let component: ServiceDeskDashboard;
  let fixture: ComponentFixture<ServiceDeskDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceDeskDashboard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ServiceDeskDashboard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
