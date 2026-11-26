import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { PartnerList } from './partner-list';

describe('PartnerList', () => {
  let fixture: ComponentFixture<PartnerList>;
  let component: PartnerList;
  let httpTesting: HttpTestingController;

  const address = { countryCode: 'HUN', postalCode: '1117', city: 'Budapest', street: 'Budafoki', publicSpace: 'Street', houseNumber: '56' };
  const contact = (id: string, vendorId: string | null) => ({
    id, firstName: 'Jane', lastName: id, jobTitle: 'Sales', phoneNumber: '+36', emailAddress: 'a@b.hu', vendorId,
  });

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PartnerList],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    fixture = TestBed.createComponent(PartnerList);
    component = fixture.componentInstance;
    httpTesting = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    httpTesting.expectOne(`${environment.api.baseUrl}/vendors`).flush([
      { id: 'v1', name: 'Tech Kft.', address, webpage: null },
      { id: 'v2', name: 'Szerviz Zrt.', address, webpage: null },
    ]);
    httpTesting.expectOne(`${environment.api.baseUrl}/contacts`).flush([
      contact('c1', 'v1'), contact('c2', 'v2'), contact('c3', null),
    ]);
    fixture.detectChanges();
  });

  afterEach(() => httpTesting.verify());

  it('shows every contact by default', () => {
    expect(component.visibleContacts().map(c => c.id)).toEqual(['c1', 'c2', 'c3']);
  });

  it('filters contacts to the selected vendor and clears the filter on second click', () => {
    component.toggleVendor('v1');
    expect(component.visibleContacts().map(c => c.id)).toEqual(['c1']);

    component.toggleVendor('v1');
    expect(component.visibleContacts().length).toBe(3);
  });

  it('shows the company name and the edit icon only for the selected rows', () => {
    const element: HTMLElement = fixture.nativeElement;
    expect(element.textContent).toContain('Tech Kft.');
    expect(element.querySelectorAll('a[aria-label]').length).toBe(0);

    component.toggleVendor('v1');
    component.toggleContact('c1');
    fixture.detectChanges();

    expect(element.querySelector('a[aria-label="Beszállító módosítása"]')).not.toBeNull();
    expect(element.querySelector('a[aria-label="Kontakt módosítása"]')).not.toBeNull();
    expect(element.textContent).toContain('a@b.hu');
  });
});
