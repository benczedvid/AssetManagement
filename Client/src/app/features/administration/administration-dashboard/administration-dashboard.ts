import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import {
  PackagesApi,
  PackageListItem,
} from '../../packages/data-access/packages-api';
import { ServiceCaseListItem, ServiceCasesApi } from '../../service-cases/data-access/service-cases-api';

@Component({
  selector: 'app-administration-dashboard',
  standalone: true,
  imports: [
    RouterLink,
  ],
  templateUrl: './administration-dashboard.html',
  styleUrl: './administration-dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdministrationDashboard implements OnInit {
  private readonly packagesApi = inject(PackagesApi);
  private readonly serviceCasesApi = inject(ServiceCasesApi);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly serviceCases = signal<readonly ServiceCaseListItem[]> ([]);
  readonly activeServiceCaseCount = computed(() => this.serviceCases().filter(serviceCase => serviceCase.status !== 'Closed').length);

  readonly packages = signal<readonly PackageListItem[]>([]);
  readonly totalPackageCount = computed(() => this.activePackageCount());

  readonly activePackageCount = computed(
    () =>
      this.packages().filter(
        item => item.status === 'Created' || item.status === 'InTransit',
      ).length);

  readonly awaitingDispatchCount = computed(() => this.packages().filter(item => item.status === 'Created').length);
  readonly awaitingReceiptCount = computed(() => this.packages().filter(item => item.status === 'InTransit').length);
  readonly totalActiveProcessCount = computed(() => this.activeServiceCaseCount() + this.activePackageCount());

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    if (this.loading()) {
      return;
    }

    this.loadPackages();
    this.loadServiceCases();
  }

  private loadPackages(): void {
    this.loading.set(true);
    this.error.set(null);

    this.packagesApi
      .getList()
      .pipe(
        finalize(() => {
          this.loading.set(false);
        }),
      )
      .subscribe({
        next: response => {
          this.packages.set(response.items);
        },
        error: error => {
          console.error('Failed to load administration packages.', error);
          this.packages.set([]);
          this.error.set('A csomagadatok betöltése nem sikerült.');
        },
      });
  }
  private loadServiceCases(): void {
    this.loading.set(true);
    this.error.set(null);

    this.serviceCasesApi
      .getList()
      .pipe(
        finalize(() => {
          this.loading.set(false);
        }),
      )
      .subscribe({
        next: response => {
          this.serviceCases.set(response.items);
        },
        error: error => {
          console.error('Failed to load administration service cases.', error);
          this.serviceCases.set([]);
          this.error.set('A szervizügyek betöltése nem sikerült.');
        },
      });
  }
}