import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NotificationService } from '../../../core/notifications/notification-service';
import { CreateVendorRequest } from '../data-access/models/create-vendor-request';
import { VendorApiService } from '../data-access/vendor-api';
import { COUNTRY_CODE_LABELS, COUNTRY_CODES, CountryCode, PUBLIC_SPACE_LABELS, PUBLIC_SPACES, PublicSpace } from '../../../shared/models/address-model';

@Component({
  selector: 'app-vendor-create',
  imports: [ReactiveFormsModule],
  templateUrl: './vendor-create.html',
  styleUrl: './vendor-create.css',
})
export class VendorCreate {
  private readonly vendorApi = inject(VendorApiService);
  private readonly router = inject(Router);
  private readonly notificationService = inject(NotificationService);

  readonly countryCodes = COUNTRY_CODES;
  readonly publicSpaces = PUBLIC_SPACES;
   readonly countryCodeLabels = COUNTRY_CODE_LABELS;
   readonly publicSpaceLabels = PUBLIC_SPACE_LABELS;
  readonly isSaving = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(200),
      ],
    }),
    countryCode: new FormControl<CountryCode>('Unknown', {
      nonNullable: true,
      validators: [
        Validators.required,
      ],
    }),
    postalCode: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(20),
        ],
    }),

    city: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(100),
      ],
    }),

    street: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(200),
      ],
    }),

    publicSpace: new FormControl<PublicSpace>('Unknown', {
      nonNullable: true,
      validators: [
        Validators.required,
      ],
    }),

    houseNumber: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(20),
      ],
    }),
    webpage: new FormControl<string | null>(null, {
      validators: [
        Validators.maxLength(500),
      ],
    }),
  });

  save(): void {
    this.saveError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();

      this.notificationService.error(
        'Please correct the validation errors before saving.',
      );

      return;
    }

    this.isSaving.set(true);

    const formValue = this.form.getRawValue();

    const request: CreateVendorRequest = {
      name: formValue.name.trim(),
      address: {
        countryCode: formValue.countryCode,
        postalCode: formValue.postalCode.trim(),
        city: formValue.city.trim(),
        street: formValue.street.trim(), 
        publicSpace: formValue.publicSpace,
        houseNumber: formValue.houseNumber.trim()
      },
      webpage: formValue.webpage?.trim() || null
    };

    this.vendorApi.create(request).subscribe({
      next: vendor => {
        this.isSaving.set(false);

        this.notificationService.success(
          'Vendor created successfully.',
        );

        void this.router.navigate(['/vendors', vendor.id]);
      },
      error: () => {
        this.isSaving.set(false);

        const errorMessage = 'The vendor could not be created.';

        this.saveError.set(errorMessage);
        this.notificationService.error(errorMessage);
      },
    });
  }

  cancel(): void {
    void this.router.navigate(['/vendors']);
  }
}