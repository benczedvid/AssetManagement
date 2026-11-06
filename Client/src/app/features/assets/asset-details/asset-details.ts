import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CurrentUserApi } from '../../auth/current-user-api';
import { ASSET_MOVEMENT_TYPE_LABELS, AssetMovementType } from '../../asset-movements/models/asset-movement-type';
import { AssetApi } from '../data-access/asset-api';
import { AssetDetailsModel } from '../models/asset-details-model';
import { ASSET_HISTORY_PROPERTY_LABELS, AssetHistoryModel } from '../models/asset-history-model';
import { ASSET_STATUS_LABELS, AssetStatus } from '../models/asset-status';
import { APPLICATION_PERMISSIONS } from '../../auth/authorization/application-permission';

@Component({
  selector: 'app-asset-details',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe
  ],
  templateUrl: './asset-details.html',
  styleUrl: './asset-details.css'
})
export class AssetDetails {
  private readonly assetApiService = inject(AssetApi);
  private readonly route = inject(ActivatedRoute);
  private readonly currentUserApi = inject(CurrentUserApi);
  readonly asset = signal<AssetDetailsModel | null>(null);
  readonly assetHistory = signal<AssetHistoryModel[]>([]);
  readonly isLoading = signal(true);
  readonly isHistoryLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly historyLoadError = signal<string | null>(null);
  readonly canManageAssets = computed(() => this.currentUserApi.canManageAssets());
  readonly hasAssignedEmployee = computed(() => {
    const currentAsset = this.asset();

    return Boolean(
      currentAsset?.assignedEmployeeName &&
      currentAsset?.assignedEmployeeNumber
    );
  });

  readonly hasAssetHistory = computed(() => this.assetHistory().length > 0);
  readonly canOpenServiceCase = computed(() =>
    this.currentUserApi.currentUser()?.isActive === true &&
    this.currentUserApi.hasAnyPermission([
      APPLICATION_PERMISSIONS.serviceCasesManageAll,
      APPLICATION_PERMISSIONS.serviceCasesManageOwnStore,
    ]),
  );

  readonly maximumDisposalReasonLength = 2000;
  readonly isDisposalFormOpen = signal(false);
  readonly isDisposing = signal(false);
  readonly disposalReason = signal('');
  readonly isDisposalConfirmed = signal(false);
  readonly disposalError = signal<string | null>(null);
  readonly disposalSuccess = signal<string | null>(null);
  readonly canDisposeAssets = computed(() => this.currentUserApi.isApplicationAdministrator());
  readonly disposalReasonLength = computed(() => this.disposalReason().trim().length);
  readonly canDisposeCurrentAsset = computed(() => {
    const currentAsset = this.asset();

    if (
      !this.canDisposeAssets() ||
      this.isLoading() ||
      this.loadError() !== null ||
      currentAsset === null
    ) {
      return false;
    }

    const hasSupportedStatus =
      currentAsset.assetStatus === 'In_Store' ||
      currentAsset.assetStatus === 'In_HQ' ||
      currentAsset.assetStatus === 'In_Service';

    return (hasSupportedStatus && currentAsset.assignedEmployeeNumber === null);
  });
  readonly canSubmitDisposal = computed(
    () =>
      this.isDisposalFormOpen() &&
      this.canDisposeCurrentAsset() &&
      !this.isDisposing() &&
      this.isDisposalConfirmed() &&
      this.disposalReasonLength() > 0 &&
      this.disposalReasonLength() <= this.maximumDisposalReasonLength,
  );

  constructor() {
    this.loadPage();
  }

  getStatusLabel(status: AssetStatus): string {
    return ASSET_STATUS_LABELS[status];
  }

  getMovementTypeLabel(movementType: AssetMovementType): string {
    return ASSET_MOVEMENT_TYPE_LABELS[movementType];
  }
  /**
   * Returns the display label for an asset history property.
   *
   * @param propertyName The property or audit metadata key.
   * @returns The corresponding display label.
   */
  getHistoryPropertyLabel(propertyName: string): string {
    if (propertyName === 'DisposalReason') {
      return 'Selejtezés indoklása';
    }

    return ASSET_HISTORY_PROPERTY_LABELS[propertyName] ?? propertyName;
  }
  getHistoryActionLabel(action: string): string {
    switch (action) {
      case 'Updated':
        return 'Módosítás';
      case 'Created':
        return 'Létrehozás';
      case 'MovementCreated':
        return 'Mozgatás létrehozva';
      case 'MovementCompleted':
        return 'Mozgatás befejezve';
      case 'Dispose':
        return 'Selejtezés';

      default:
        return action;
    }
  }

  getHistoryValue(value: string | null): string {
    if (value === null || value.length === 0) {
      return 'Nincs érték';
    }

    return value;
  }

  getStatusClass(status: AssetStatus): string {
    switch (status) {
      case 'In_HQ':
        return 'bg-lime-50 text-lime-700';
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

  getMovementDescription(type: AssetMovementType): string {
    switch (type) {
      case 'Checkout':
        return 'A dolgozó felvette az eszközt.';
      case 'Return':
        return 'A dolgozó leadta az eszközt.';
      default:
        return 'Eszközmozgás történt.';
    }
  }

  reloadHistory(): void {
    const assetId = this.route.snapshot.paramMap.get('id');

    if (!assetId) {
      this.historyLoadError.set('Az eszköz azonosítója hiányzik az útvonalból.');

      return;
    }

    this.loadAssetHistory(assetId);
  }

  private loadPage(): void {
    const assetId =this.route.snapshot.paramMap.get('id');

    if (!assetId) {
      this.isLoading.set(false);

      this.loadError.set('Az eszköz azonosítója hiányzik az útvonalból.');

      return;
    }

    this.loadAssetDetails(assetId);
    this.loadAssetHistory(assetId);
  }

  private loadAssetDetails(assetId: string): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.assetApiService
      .getDetails(assetId)
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        })
      )
      .subscribe({
        next: asset => {
          this.asset.set(asset);
        },

        error: (
          error: HttpErrorResponse
        ) => {
          console.error('Az eszköz adatainak betöltése sikertelen.', error);

          this.loadError.set(this.getAssetErrorMessage(error)
          );
        }
      });
  }

  private loadAssetHistory(assetId: string): void {
    this.isHistoryLoading.set(true);
    this.historyLoadError.set(null);

    this.assetApiService
      .getAssetHistory(assetId)
      .pipe(
        finalize(() => {
          this.isHistoryLoading.set(false);
        })
      )
      .subscribe({
        next: history => {
          this.assetHistory.set(history);
        },

        error: (
          error: HttpErrorResponse
        ) => {
          console.error('Az eszköz módosítási előzményeinek betöltése sikertelen.', error);

          this.assetHistory.set([]);

          this.historyLoadError.set(
            this.getHistoryErrorMessage(error)
          );
        }
      });
  }
  /**
   * Opens the disposal form for an eligible asset.
   * Previous form values and notifications are cleared.
   */
  openDisposalForm(): void {
    if (!this.canDisposeCurrentAsset() || this.isDisposing()) {
      return;
    }

    this.disposalReason.set('');
    this.isDisposalConfirmed.set(false);
    this.disposalError.set(null);
    this.disposalSuccess.set(null);
    this.isDisposalFormOpen.set(true);
  }

  /**
   * Closes the disposal form unless a request is in progress.
   */
  closeDisposalForm(): void {
    if (this.isDisposing()) {
      return;
    }

    this.isDisposalFormOpen.set(false);
    this.disposalReason.set('');
    this.isDisposalConfirmed.set(false);
    this.disposalError.set(null);
  }

  /**
   * Updates the reason and invalidates any previous confirmation.
   *
   * @param reason The untrimmed reason entered by the administrator.
   */
  updateDisposalReason(reason: string): void {
    if (this.isDisposing()) {
      return;
    }

    this.disposalReason.set(reason);
    this.isDisposalConfirmed.set(false);
    this.disposalError.set(null);
  }

  /**
   * Updates the explicit disposal confirmation.
   *
   * @param confirmed Whether the administrator confirmed disposal.
   */
  updateDisposalConfirmation(confirmed: boolean): void {
    if (this.isDisposing()) {
      return;
    }

    this.isDisposalConfirmed.set(confirmed);
  }

  /**
   * Submits a standalone disposal request after local validation.
   *
   * The backend performs the authoritative authorization and state checks.
   * The request is not automatically retried.
   *
   * On success, the returned status is applied immediately and the asset
   * details and history are reloaded from the server.
   *
   * @param event The form submission event.
   */
  submitDisposal(event: Event): void {
    event.preventDefault();

    if (this.isDisposing()) {
      return;
    }

    this.disposalError.set(null);

    const currentAsset = this.asset();

    if (currentAsset === null || !this.canDisposeCurrentAsset()) {
      this.disposalError.set('The current user or asset state does not allow standalone disposal.');
      return;
    }

    const reason = this.disposalReason().trim();

    if (reason.length === 0) {
      this.disposalError.set('A disposal reason is required.');
      return;
    }

    if (reason.length > this.maximumDisposalReasonLength) {
      this.disposalError.set(`The disposal reason cannot exceed ${this.maximumDisposalReasonLength} characters after trimming.`);
      return;
    }

    if (!this.isDisposalConfirmed()) {
      this.disposalError.set( 'Confirm the disposal before submitting the request.');
      return;
    }

    this.isDisposing.set(true);
    this.disposalSuccess.set(null);

    this.assetApiService
      .dispose(currentAsset.assetId, { reason })
      .pipe(
        finalize(() => {
          this.isDisposing.set(false);
        }),
      )
      .subscribe({
        next: response => {
          // Apply the confirmed status even if a subsequent reload fails.
          this.asset.update(asset =>
            asset?.assetId === response.assetId
              ? { ...asset, assetStatus: response.assetStatus }
              : asset
          );

          this.isDisposalFormOpen.set(false);
          this.disposalReason.set('');
          this.isDisposalConfirmed.set(false);
          this.disposalError.set(null);
          this.disposalSuccess.set('Eszköz sikeresen selejtezve');

          this.loadAssetDetails(response.assetId);
          this.loadAssetHistory(response.assetId);
        },
        error: (error: HttpErrorResponse) => {
          // Require a new explicit confirmation before another attempt.
          this.isDisposalConfirmed.set(false);
          this.disposalError.set(this.getDisposalErrorMessage(error));
        },
      });
  }

  /**
   * Returns a safe user-facing message for a disposal request failure.
   * Raw server exception messages are not displayed.
   *
   * @param error The HTTP error returned by the request.
   * @returns A description of the failure and any relevant next step.
   */
  private getDisposalErrorMessage(error: HttpErrorResponse): string {
    switch (error.status) {
      case 0:
        return (
          'The disposal result could not be confirmed. Reload the asset and check its history before trying again.'
        );
      case 400:
        return (
          'The asset identifier or disposal reason is invalid. The reason must contain between 1 and 2000 characters after trimming.'
        );
      case 401:
        return 'Sign in to dispose of the asset.';
      case 403:
        return (
          'Access denied. Only active application administrators with the required permission can dispose of assets.'
        );
      case 404:
        return 'The requested asset was not found.';
      case 409:
        return (
          'The asset cannot be disposed of in its current state, ' +
          'is still assigned to a user, or has an active service case. ' +
          'If an active service case exists, dispose of the asset ' +
          'through that service case.'
        );
      default:
        return (
          'The disposal request failed. Reload the asset and check its history before trying again.'
        );
    }
  }

  private getAssetErrorMessage(error: HttpErrorResponse): string {
    switch (error.status) {
      case 0:
        return 'A szerver nem érhető el. Ellenőrizd a hálózati kapcsolatot.';
      case 400:
        return 'Az eszközazonosító érvénytelen.';
      case 401:
        return 'Az oldal megtekintéséhez bejelentkezés szükséges.';
      case 403:
        return 'Nincs jogosultságod az eszköz megtekintéséhez.';
      case 404:
        return 'A keresett eszköz nem található.';
      default:
        return 'Az eszköz adatainak betöltése sikertelen.';
    }
  }

  private getHistoryErrorMessage(error: HttpErrorResponse): string {
    switch (error.status) {
      case 0:
        return 'A szerver nem érhető el. Az előzmények nem tölthetők be.';
      case 400:
        return 'Az eszközazonosító érvénytelen.';
      case 401:
        return 'Az előzmények megtekintéséhez bejelentkezés szükséges.';
      case 403:
        return 'Nincs jogosultságod az eszköz előzményeinek megtekintéséhez.';
      case 404:
        return 'Az eszköz vagy annak előzményei nem találhatók.';
      default:
        return 'Az eszköz módosítási előzményeinek betöltése sikertelen.';
    }
  }
}