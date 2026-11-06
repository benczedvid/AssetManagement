import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StoreList } from './store-list';

describe('StoreList', () => {
  let component: StoreList;
  let fixture: ComponentFixture<StoreList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StoreList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StoreList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
