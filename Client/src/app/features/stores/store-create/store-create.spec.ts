import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StoreCreate } from './store-create';

describe('StoreCreate', () => {
  let component: StoreCreate;
  let fixture: ComponentFixture<StoreCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StoreCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StoreCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
