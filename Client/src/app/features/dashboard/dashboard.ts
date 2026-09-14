import { Component, inject, OnInit, signal } from '@angular/core';

import { DashboardApiService } from './data-access/dashboard-api';
import { StoreDashboardResponse } from './data-access/models/store-dashboard-response';
import { RouterLink } from "@angular/router";

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit {
  private readonly dashboardApiService =
    inject(DashboardApiService);

  readonly stores = signal<StoreDashboardResponse[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    if (this.isLoading() && this.stores().length > 0) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.dashboardApiService
      .getStoreDashboard()
      .subscribe({
        next: stores => {
          this.stores.set(stores);
          this.isLoading.set(false);
        },
        error: error => {
          console.error(
            'A dashboard betöltése sikertelen.',
            error
          );

          this.errorMessage.set(
            'A dashboard adatait nem sikerült betölteni.'
          );

          this.isLoading.set(false);
        }
      });
  }
}
