import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PackageAssetsEditor } from './package-assets-editor';

describe('PackageAssetsEditor', () => {
  let component: PackageAssetsEditor;
  let fixture: ComponentFixture<PackageAssetsEditor>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PackageAssetsEditor]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PackageAssetsEditor);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
