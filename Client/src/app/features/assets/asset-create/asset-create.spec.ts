import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AssetCreate } from './asset-create';

describe('AssetCreate', () => {
  let component: AssetCreate;
  let fixture: ComponentFixture<AssetCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssetCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AssetCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
