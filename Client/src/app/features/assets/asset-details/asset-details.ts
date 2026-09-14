import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

import {
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';

import {
  ActivatedRoute,
  RouterLink,
} from '@angular/router';

import { finalize } from 'rxjs';

import { CurrentUserApi } from
  '../../auth/current-user-api';

import {
  ASSET_MOVEMENT_TYPE_LABELS,
  AssetMovementType,
} from
  '../../asset-movements/data-access/models/asset-movement-type';

import { AssetApi } from
  '../data-access/asset-api';

import { AssetDetailsModel } from
  '../data-access/models/asset-details-model';

import {
  ASSET_STATUS_LABELS,
  AssetStatus,
} from
  '../data-access/models/asset-status';

@Component({
  selector: 'app-asset-details',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
  ],
  templateUrl: './asset-details.html',
  styleUrl: './asset-details.css',
})
export class AssetDetails {
  private readonly assetApiService =
    inject(AssetApi);

  private readonly route =
    inject(ActivatedRoute);

  private readonly currentUserApi =
    inject(CurrentUserApi);

  readonly asset =
    signal<AssetDetailsModel | null>(null);

  readonly isLoading =
    signal(true);

  readonly loadError =
    signal<string | null>(null);

  readonly canManageAssets = computed(
    () =>
      this.currentUserApi.canManageAssets()
  );

  readonly hasAssignedEmployee = computed(
    () => {
      const currentAsset = this.asset();

      return Boolean(
        currentAsset?.assignedEmployeeName &&
        currentAsset?.assignedEmployeeNumber
      );
    }
  );

  constructor() {
    this.loadAssetDetails();
  }

  getStatusLabel(
    status: AssetStatus
  ): string {
    return ASSET_STATUS_LABELS[status];
  }

  getMovementTypeLabel(
    movementType: AssetMovementType
  ): string {
    return ASSET_MOVEMENT_TYPE_LABELS[
      movementType
    ];
  }

  getStatusClass(
    status: AssetStatus
  ): string {
    switch (status) {
      case 'In_HQ':
      case 'In_Store':
        return 'bg-emerald-50 text-emerald-700';

      case 'Used_In_HQ':
      case 'Used_In_Store':
        return 'bg-blue-50 text-blue-700';

      case 'Route_To_Store':
      case 'Route_To_HQ':
      case 'Route_To_Service':
        return 'bg-yellow-50 text-yellow-700';

      case 'In_Service':
        return 'bg-violet-50 text-violet-700';

      case 'Disposed':
        return 'bg-slate-100 text-slate-600';

      case 'Unknown':
      default:
        return 'bg-red-50 text-red-700';
    }
  }

  getMovementDescription(
    type: AssetMovementType
  ): string {
    switch (type) {
      case 'Checkout':
        return 'A dolgozó felvette az eszközt.';

      case 'Return':
        return 'A dolgozó leadta az eszközt.';

      default:
        return 'Eszközmozgás történt.';
    }
  }

  private loadAssetDetails(): void {
    const assetId =
      this.route.snapshot.paramMap.get('id');

    if (!assetId) {
      this.isLoading.set(false);

      this.loadError.set(
        'Az eszköz azonosítója hiányzik az útvonalból.'
      );

      return;
    }

    this.isLoading.set(true);
    this.loadError.set(null);

    this.assetApiService
      .getDetails(assetId)
      .pipe(
        finalize(
          () =>
            this.isLoading.set(false)
        )
      )
      .subscribe({
        next: asset => {
          this.asset.set(asset);
        },

        error: (
          error: HttpErrorResponse
        ) => {
          console.error(
            'Az eszköz adatainak betöltése sikertelen.',
            error
          );

          this.loadError.set(
            this.getErrorMessage(error)
          );
        },
      });
  }

  private getErrorMessage(
    error: HttpErrorResponse
  ): string {
    switch (error.status) {
      case 0:
        return (
          'A szerver nem érhető el. ' +
          'Ellenőrizd a hálózati kapcsolatot.'
        );

      case 400:
        return 'Az eszközazonosító érvénytelen.';

      case 401:
        return (
          'Az oldal megtekintéséhez ' +
          'bejelentkezés szükséges.'
        );

      case 403:
        return (
          'Nincs jogosultságod az eszköz ' +
          'megtekintéséhez.'
        );

      case 404:
        return 'A keresett eszköz nem található.';

      default:
        return (
          'Az eszköz adatainak betöltése ' +
          'sikertelen.'
        );
    }
  }
}