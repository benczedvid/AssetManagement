import { Component, inject, signal } from '@angular/core';
import { StoreApiService } from '../data-access/store-api';
import { GetStoresResponse } from '../data-access/models/get-stores-response';
import { RouterLink } from '@angular/router';
import { COUNTRY_CODE_LABELS, PUBLIC_SPACE_LABELS } from '../../../shared/models/address-model';


@Component({
  selector: 'app-store-list',
  imports: [RouterLink],
  templateUrl: './store-list.html',
  styleUrl: './store-list.css',
})
export class StoreList {
  private readonly storeApiService = inject(StoreApiService);
  readonly stores = signal<readonly GetStoresResponse[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly publicSpaceLabels = PUBLIC_SPACE_LABELS;
  readonly countryCodeLabels = COUNTRY_CODE_LABELS;

  ngOnInit(): void {
    this.loadStores();
  }

  loadStores(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.storeApiService.getAll().subscribe({
      next: stores => {
        this.stores.set(stores);
        const sortedStores = [...stores].sort(
          (a, b) =>
            parseInt(a.storeNumber, 10) -
            parseInt(b.storeNumber, 10)
        );

        this.stores.set(sortedStores);
        this.isLoading.set(false);
      },
      error: error => {
        console.error("Az áruházlista betöltése sikertelen volt.");
        this.errorMessage.set("Az áruházak jelenleg nem tölthetőek be.");
        this.isLoading.set(false);
      }
    });
  }
}
