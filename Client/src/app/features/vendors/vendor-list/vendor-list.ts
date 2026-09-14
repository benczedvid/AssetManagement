import { Component, inject, OnInit, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { COUNTRY_CODE_LABELS, PUBLIC_SPACE_LABELS } from '../../../shared/models/address-model';
import { VendorApiService } from '../data-access/vendor-api';
import { GetVendorsResponse } from '../data-access/models/get-vendors-response';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-vendor-list',
  imports: [RouterLink],
  templateUrl: './vendor-list.html',
  styleUrl: './vendor-list.css',
})
export class VendorList implements OnInit {
  private readonly vendorApiService = inject(VendorApiService);

  readonly vendors = signal<readonly GetVendorsResponse[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly publicSpaceLabels = PUBLIC_SPACE_LABELS;
  readonly countryCodeLabels = COUNTRY_CODE_LABELS;

  ngOnInit(): void {
    this.loadVendors();
  }

  loadVendors(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.vendorApiService
      .getAll()
      .pipe(
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: vendors => {
          this.vendors.set(vendors);
        },
        error: error => {
          console.error('Failed to load the vendor list.', error);

          this.errorMessage.set(
            'Vendors are currently unavailable.',
          );
        },
      });
  }
}
