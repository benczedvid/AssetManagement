import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsDashboard } from './settings-dashboard';

describe('SettingsDashboard', () => {
  let component: SettingsDashboard;
  let fixture: ComponentFixture<SettingsDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SettingsDashboard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SettingsDashboard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
