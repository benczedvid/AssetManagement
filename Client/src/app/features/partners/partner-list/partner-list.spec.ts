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
    id, firstName: 'Jane', lastName: id, jobTitle: 'Sales', phoneNumber: '+36 30 123 4567', emailAddress: 'a@b.hu', vendorId,
  });
  const ids = (items: readonly { id: string }[]) => items.map(item => item.id);

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
      contact('c1', 'v1'),
      { ...contact('c2', 'v2'), lastName: 'Kovács', phoneNumber: '+36 20 555 1234', emailAddress: 'kovacs@szerviz.hu' },
      contact('c3', null),
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

  it('searches by vendor name, contact name, phone number and e-mail address', () => {
    component.search('tech');
    expect(ids(component.visibleVendors())).toEqual(['v1']);
    expect(ids(component.visibleContacts())).toEqual(['c1']);

    component.search('kovacs');
    expect(ids(component.visibleContacts())).toEqual(['c2']);
    expect(ids(component.visibleVendors())).toEqual(['v2']);

    component.search('20 5551');
    expect(ids(component.visibleContacts())).toEqual(['c2']);

    component.search('@SZERVIZ.hu');
    expect(ids(component.visibleContacts())).toEqual(['c2']);

    component.search('');
    expect(component.visibleContacts().length).toBe(3);
    expect(component.visibleVendors().length).toBe(2);
  });

  it('combines the search with the selected vendor', () => {
    component.toggleVendor('v1');
    component.search('kovacs');

    expect(component.visibleContacts().length).toBe(0);
  });

  it('shows the full vendor address with the Hungarian public space label', () => {
    expect(fixture.nativeElement.textContent).toContain('1117 Budapest, Budafoki utca 56.');
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
