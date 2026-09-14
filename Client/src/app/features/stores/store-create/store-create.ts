import { Component, inject, signal } from '@angular/core';
import { StoreApiService } from '../data-access/store-api';
import { Router, RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateStoreRequest } from '../data-access/models/create-store-request';
import { COUNTRY_CODES, PUBLIC_SPACES, COUNTRY_CODE_LABELS, CountryCode, PublicSpace, PUBLIC_SPACE_LABELS } from '../../../shared/models/address-model';

@Component({
  selector: 'app-store-create',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './store-create.html',
  styleUrl: './store-create.css',
})
export class StoreCreate {
  private readonly storeApiService = inject(StoreApiService);
  private readonly router = inject(Router);

  readonly isSaving = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly countryCodes = COUNTRY_CODES;
  readonly publicSpaces = PUBLIC_SPACES;
  readonly publicSpaceLabels = PUBLIC_SPACE_LABELS;
  readonly countryCodeLabels = COUNTRY_CODE_LABELS;
  readonly form = new FormGroup({
    storeNumber: new FormControl('',
      {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(5)]
    }),
    name: new FormControl('',
      {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(200)]
      }),
    countryCode: new FormControl<CountryCode>('Unknown',
      {
        nonNullable: true,
        validators: [Validators.required]
      }),
    postalCode: new FormControl('',
      {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(10)]
      }),
    city: new FormControl('',
      {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(200)]
      }),
    street: new FormControl('',
      {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(200)]
      }),
    publicSpace: new FormControl<PublicSpace>('Unknown',
      {
        nonNullable: true,
        validators: [Validators.required]
      }),
    houseNumber: new FormControl('',
      {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(10)]
      })
  });

  save(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();

    const request: CreateStoreRequest = {
      storeNumber: value.storeNumber,
      name: value.name,
      address: {
      countryCode: value.countryCode,
      postalCode: value.postalCode,
      city: value.city,
      street: value.street,
      publicSpace: value.publicSpace,
      houseNumber: value.houseNumber
      }
    };

    this.isSaving.set(true);
    this.saveError.set(null);

    this.storeApiService.create(request).subscribe({
      next: () => {
        this.isSaving.set(false);

        void this.router.navigate(["/stores"]);
      },
      error: error => {
        console.error("Az áruház létrehozása sikertelen.", error);
        this.saveError.set("Az áruházat nem sikerült létrehozni.");
        this.isSaving.set(false);
      }
    })
  }
}
