import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ServiceCasesList } from './service-cases-list';

describe('ServiceCasesList', () => {
  let component: ServiceCasesList;
  let fixture: ComponentFixture<ServiceCasesList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceCasesList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ServiceCasesList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
