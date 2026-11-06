import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SideMenuComponent } from './side-menu';

describe('SideMenuComponent', () => {
  let component: SideMenuComponent;
  let fixture: ComponentFixture<SideMenuComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SideMenuComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(SideMenuComponent);
    component = fixture.componentInstance;

    fixture.componentRef.setInput('title', 'Típus');
    fixture.componentRef.setInput('items', [
      {
        label: 'Összes eszköz',
        key: null,
        route: '/asset-list',
        queryParams: { type: null },
      },
    ]);

    fixture.detectChanges();
  });

  it('létrejön', () => {
    expect(component).toBeTruthy();
  });

  it('összecsukja és kinyitja a menüpontokat', () => {
    const button: HTMLButtonElement =
      fixture.nativeElement.querySelector('.side-menu-heading');

    expect(fixture.nativeElement.querySelector('.side-menu-items'))
      .not.toBeNull();

    button.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.side-menu-items'))
      .toBeNull();

    button.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.side-menu-items'))
      .not.toBeNull();
  });
});