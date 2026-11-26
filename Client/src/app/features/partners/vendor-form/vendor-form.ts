import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/notifications/notification-service';
import { COUNTRY_CODE_LABELS, COUNTRY_CODES, CountryCode, PUBLIC_SPACE_LABELS, PUBLIC_SPACES, PublicSpace } from '../../../shared/models/address-model';
import { SaveVendorRequest } from '../../vendors/data-access/models/save-vendor-request';
import { VendorApiService } from '../../vendors/data-access/vendor-api';

/** Create and edit form for vendors; edit mode when the route has an :id. */
@Component({
  selector: 'app-vendor-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './vendor-form.html',
})
export class VendorForm implements OnInit {
  private readonly vendorApi = inject(VendorApiService);
  private readonly router = inject(Router);
  private readonly notificationService = inject(NotificationService);

  readonly vendorId = inject(ActivatedRoute).snapshot.paramMap.get('id');
  readonly countryCodes = COUNTRY_CODES;
  readonly publicSpaces = PUBLIC_SPACES;
  readonly countryCodeLabels = COUNTRY_CODE_LABELS;
  readonly publicSpaceLabels = PUBLIC_SPACE_LABELS;
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    countryCode: new FormControl<CountryCode>('Unknown', { nonNullable: true, validators: [Validators.required] }),
    postalCode: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(10)] }),
    city: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    street: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    publicSpace: new FormControl<PublicSpace>('Unknown', { nonNullable: true, validators: [Validators.required] }),
    houseNumber: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(10)] }),
    webpage: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2048)] }),
  });

  ngOnInit(): void {
    if (!this.vendorId) {
      return;
    }

    this.isLoading.set(true);

    this.vendorApi
      .getById(this.vendorId)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: vendor => this.form.setValue({ name: vendor.name, ...vendor.address, webpage: vendor.webpage ?? '' }),
        error: () => this.saveError.set('A beszállító adatai nem tölthetők be.'),
      });
  }

  save(): void {
    this.saveError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notificationService.error('Kérjük, javítsa a hibás mezőket mentés előtt.');
      return;
    }

    const { name, webpage, ...address } = this.form.getRawValue();

    const request: SaveVendorRequest = {
      name: name.trim(),
      address: {
        ...address,
        postalCode: address.postalCode.trim(),
        city: address.city.trim(),
        street: address.street.trim(),
        houseNumber: address.houseNumber.trim(),
      },
      webpage: webpage.trim() || null,
    };

    this.isSaving.set(true);

    const save$ = this.vendorId ? this.vendorApi.update(this.vendorId, request) : this.vendorApi.create(request);

    save$.pipe(finalize(() => this.isSaving.set(false))).subscribe({
      next: () => {
        this.notificationService.success('A beszállító mentése sikerült.');
        void this.router.navigate(['/partners']);
      },
      error: () => {
        const message = 'A beszállító mentése nem sikerült.';
        this.saveError.set(message);
        this.notificationService.error(message);
      },
    });
  }
}
