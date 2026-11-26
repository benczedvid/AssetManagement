import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { concat, finalize, forkJoin, last, Observable, of, switchMap } from 'rxjs';
import { NotificationService } from '../../../core/notifications/notification-service';
import { GetVendorsResponse } from '../../vendors/data-access/models/get-vendors-response';
import { VendorApiService } from '../../vendors/data-access/vendor-api';
import { ContactApiService } from '../data-access/contact-api';
import { SaveContactRequest } from '../data-access/models/contact';
import { PHONE_NUMBER_EXAMPLE, PHONE_NUMBER_PATTERN } from '../data-access/phone-number';

/**
 * Create and edit form for contacts; edit mode when the route has an :id.
 * The optional vendor selection attaches the contact to (or detaches it from) a vendor.
 */
@Component({
  selector: 'app-contact-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './contact-form.html',
})
export class ContactForm implements OnInit {
  private readonly contactApi = inject(ContactApiService);
  private readonly vendorApi = inject(VendorApiService);
  private readonly router = inject(Router);
  private readonly notificationService = inject(NotificationService);
  private readonly route = inject(ActivatedRoute).snapshot;

  readonly contactId = this.route.paramMap.get('id');
  readonly phoneNumberExample = PHONE_NUMBER_EXAMPLE;
  readonly vendors = signal<readonly GetVendorsResponse[]>([]);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly saveError = signal<string | null>(null);

  /** The vendor the contact was attached to when the form was loaded. */
  private originalVendorId: string | null = null;

  readonly form = new FormGroup({
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    jobTitle: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(30), Validators.pattern(PHONE_NUMBER_PATTERN)] }),
    emailAddress: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email, Validators.maxLength(200)] }),
    vendorId: new FormControl<string | null>(this.route.queryParamMap.get('vendorId')),
  });

  ngOnInit(): void {
    forkJoin({
      vendors: this.vendorApi.getAll(),
      contact: this.contactId ? this.contactApi.getById(this.contactId) : of(null),
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ vendors, contact }) => {
          this.vendors.set(vendors);

          if (contact) {
            const { id, ...values } = contact;
            this.originalVendorId = contact.vendorId;
            this.form.setValue(values);
          }
        },
        error: () => this.saveError.set('A kontakt adatai nem tölthetők be.'),
      });
  }

  save(): void {
    this.saveError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notificationService.error('Kérjük, javítsa a hibás mezőket mentés előtt.');
      return;
    }

    const { vendorId, ...values } = this.form.getRawValue();
    const request: SaveContactRequest = {
      lastName: values.lastName.trim(),
      firstName: values.firstName.trim(),
      jobTitle: values.jobTitle.trim(),
      phoneNumber: values.phoneNumber.trim(),
      emailAddress: values.emailAddress.trim(),
    };

    this.isSaving.set(true);

    const save$ = this.contactId ? this.contactApi.update(this.contactId, request) : this.contactApi.create(request);

    save$
      .pipe(
        switchMap(contact => this.syncVendor(contact.id, vendorId || null)),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: () => {
          this.notificationService.success('A kontakt mentése sikerült.');
          void this.router.navigate(['/partners']);
        },
        error: error => {
          const message = error?.status === 409
            ? 'A kontakt nem csatolható: a beszállítónál már van kontakt ezzel a telefonszámmal.'
            : 'A kontakt mentése nem sikerült.';
          this.saveError.set(message);
          this.notificationService.error(message);
        },
      });
  }

  /** Detaches the contact from its previous vendor and attaches it to the selected one, when they differ. */
  private syncVendor(contactId: string, vendorId: string | null): Observable<unknown> {
    if (vendorId === this.originalVendorId) {
      return of(null);
    }

    const steps: Observable<unknown>[] = [];

    if (this.originalVendorId) {
      steps.push(this.vendorApi.detachContact(this.originalVendorId, contactId));
    }

    if (vendorId) {
      steps.push(this.vendorApi.attachContact(vendorId, contactId));
    }

    return concat(...steps).pipe(last(undefined, null));
  }
}
