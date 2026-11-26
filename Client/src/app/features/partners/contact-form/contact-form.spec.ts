import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { ContactForm } from './contact-form';

describe('ContactForm', () => {
  let httpTesting: HttpTestingController;
  const api = environment.api.baseUrl;
  const values = { lastName: 'Smith', firstName: 'Jane', jobTitle: 'Sales', phoneNumber: '+36 30 123 4567', emailAddress: 'a@b.hu' };

  function create(id: string | null, query: Record<string, string> = {}) {
    TestBed.configureTestingModule({
      imports: [ContactForm],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}), queryParamMap: convertToParamMap(query) } } },
      ],
    });
    spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
    httpTesting = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ContactForm);
    fixture.detectChanges();
    httpTesting.expectOne(`${api}/vendors`).flush([]);
    return fixture.componentInstance;
  }

  afterEach(() => httpTesting.verify());

  it('creates the contact and attaches it to the vendor passed in the query', () => {
    const component = create(null, { vendorId: 'v1' });
    component.form.patchValue(values);

    component.save();

    httpTesting.expectOne({ method: 'POST', url: `${api}/contacts` }).flush({ id: 'c1', ...values, vendorId: null });
    httpTesting.expectOne({ method: 'PUT', url: `${api}/vendors/v1/contacts/c1` }).flush(null);
  });

  it('moves the contact to another vendor when the selection changes', () => {
    const component = create('c1');
    httpTesting.expectOne(`${api}/contacts/c1`).flush({ id: 'c1', ...values, vendorId: 'v1' });
    component.form.controls.vendorId.setValue('v2');

    component.save();

    httpTesting.expectOne({ method: 'PUT', url: `${api}/contacts/c1` }).flush({ id: 'c1', ...values, vendorId: 'v1' });
    httpTesting.expectOne({ method: 'DELETE', url: `${api}/vendors/v1/contacts/c1` }).flush(null);
    httpTesting.expectOne({ method: 'PUT', url: `${api}/vendors/v2/contacts/c1` }).flush(null);
  });

  it('does not save when the phone number is not in international format', () => {
    const component = create(null);
    component.form.patchValue({ ...values, phoneNumber: '06 30 123 4567' });

    component.save();

    expect(component.form.controls.phoneNumber.hasError('pattern')).toBeTrue();
  });

  it('does not touch the vendor link when the selection is unchanged', () => {
    const component = create('c1');
    httpTesting.expectOne(`${api}/contacts/c1`).flush({ id: 'c1', ...values, vendorId: 'v1' });

    component.save();

    httpTesting.expectOne({ method: 'PUT', url: `${api}/contacts/c1` }).flush({ id: 'c1', ...values, vendorId: 'v1' });
  });
});
