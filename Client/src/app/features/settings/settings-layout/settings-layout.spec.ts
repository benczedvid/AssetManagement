import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsLayout } from './settings-layout';

describe('SettingsLayout', () => {
  let component: SettingsLayout;
  let fixture: ComponentFixture<SettingsLayout>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SettingsLayout]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SettingsLayout);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
