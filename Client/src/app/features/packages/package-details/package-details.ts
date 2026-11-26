import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  TemplateRef,
  ViewChild,
  computed,
  inject,
  input,
  output,
  signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { A11yModule } from '@angular/cdk/a11y';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import { getPackageEventTypeLabel, getPackageStatusLabel, PackagesApi } from '../data-access/packages-api';
import type { AssignCourierResponse, PackageDetails as PackageDetailsModel, PackageEvent, PackageStatus } from '../data-access/packages-api';

import { PackageAssignCourier } from '../package-assign-courier/package-assign-courier';
import { PackageAssetsEditor } from '../package-assets-editor/package-assets-editor';

interface RemoveAssetDialogData {
  packageId: string;
  assetId: string;
  assetName: string;
  serialNumber: string | null;
}

@Component({
  selector: 'app-package-details',
  standalone: true,
  imports: [
    DatePipe,
    A11yModule,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatIconModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatTableModule,
    PackageAssignCourier,
    PackageAssetsEditor,
  ],
  templateUrl: './package-details.html',
  styleUrl: './package-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageDetails {
  private readonly packagesApi = inject(PackagesApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  @ViewChild('courierDialog', { static: true })
  private courierDialog!: TemplateRef<unknown>;

  @ViewChild('assetsDialog', { static: true })
  private assetsDialog!: TemplateRef<unknown>;

  @ViewChild('removeAssetDialog', { static: true })
  private removeAssetDialog!: TemplateRef<{
    $implicit: RemoveAssetDialogData;
  }>;

  private courierDialogRef: MatDialogRef<unknown, void> | null = null;
  private assetsDialogRef: MatDialogRef<unknown, void> | null = null;
  private removeAssetDialogRef: MatDialogRef<unknown, boolean> | null = null;
  readonly packageDetails = input.required<PackageDetailsModel>();

  readonly canAssignCourier = input(false);
  readonly canManageAssets = input(false);
  readonly assetsChanged = output<void>();
  readonly courierAssigned = output<AssignCourierResponse>();
  readonly assetOperationInProgress = signal(false);

  readonly sortedItems = computed(() =>
    [...this.packageDetails().items].sort((left, right) => {
      const dateComparison = left.addedAtUtc.localeCompare(right.addedAtUtc);

      return dateComparison !== 0 ? dateComparison : left.id.localeCompare(right.id);
    }),
  );

  readonly sortedEvents = computed(() =>
    [...this.packageDetails().events].sort((left, right) => {
      const dateComparison =
        right.occurredAtUtc.localeCompare(left.occurredAtUtc);

      return dateComparison !== 0 ? dateComparison : right.id.localeCompare(left.id);
    }),
  );

  readonly canEditAssets = computed(() => this.packageDetails().status === 'Created' && this.canManageAssets());
  readonly canEditCourier = computed(() =>
      this.packageDetails().status === 'Created' &&
      this.canAssignCourier(),
  );

  readonly assetColumns = computed(() =>
    this.canEditAssets()
      ? ['asset', 'addedAt', 'actions']
      : ['asset', 'addedAt'],
  );

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.courierDialogRef?.close();
      this.assetsDialogRef?.close();
      this.removeAssetDialogRef?.close(false);
    });
  }

  openCourierEditor(): void {
    if (
      this.destroyRef.destroyed ||
      !this.canEditCourier() ||
      this.assetOperationInProgress() ||
      this.courierDialogRef ||
      this.assetsDialogRef ||
      this.removeAssetDialogRef
    ) {
      return;
    }

    const dialogRef =
      this.dialog.open<unknown, unknown, void>(
        this.courierDialog,
        {
          width: '640px',
          maxWidth: '96vw',
          maxHeight: '90dvh',
          ariaLabelledBy: 'package-courier-dialog-title',
          autoFocus: 'first-tabbable',
          restoreFocus: true,
        },
      );

    this.courierDialogRef = dialogRef;

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.courierDialogRef === dialogRef) {
          this.courierDialogRef = null;
        }
      });
  }

  closeCourierEditor(): void {
    this.courierDialogRef?.close();
  }

  onCourierAssigned(response: AssignCourierResponse): void {
    this.closeCourierEditor();

    // The parent displays the success snackbar and reloads data.
    this.courierAssigned.emit(response);
  }

  openAssetsEditor(): void {
    if (
      this.destroyRef.destroyed ||
      !this.canEditAssets() ||
      this.assetOperationInProgress() ||
      this.assetsDialogRef ||
      this.courierDialogRef ||
      this.removeAssetDialogRef
    ) {
      return;
    }

    const dialogRef =
      this.dialog.open<unknown, unknown, void>(
        this.assetsDialog,
        {
          width: '800px',
          maxWidth: '96vw',
          maxHeight: '90dvh',
          ariaLabelledBy: 'package-assets-dialog-title',
          autoFocus: 'first-tabbable',
          restoreFocus: true,
        },
      );

    this.assetsDialogRef = dialogRef;

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.assetsDialogRef === dialogRef) {
          this.assetsDialogRef = null;
        }
      });
  }

  closeAssetsEditor(): void {
    this.assetsDialogRef?.close();
  }

  onAssetsChanged(): void {
    this.closeAssetsEditor();

    // The parent displays the success snackbar and reloads data.
    this.assetsChanged.emit();
  }

  requestRemoveAsset(assetId: string): void {
    if (
      this.destroyRef.destroyed ||
      !this.canEditAssets() ||
      this.assetOperationInProgress() ||
      this.removeAssetDialogRef ||
      this.assetsDialogRef ||
      this.courierDialogRef
    ) {
      return;
    }

    const details = this.packageDetails();

    const item = details.items.find(asset => asset.assetId === assetId);

    if (!item) {
      return;
    }

    const data: RemoveAssetDialogData = {
      packageId: details.id,
      assetId: item.assetId,
      assetName: item.assetName,
      serialNumber: item.serialNumber || null,
    };

    const dialogRef = this.dialog.open<unknown, RemoveAssetDialogData, boolean>(this.removeAssetDialog, {
      data,
      width: '460px',
      maxWidth: '94vw',
      ariaLabelledBy: 'package-remove-asset-dialog-title',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
    });

    this.removeAssetDialogRef = dialogRef;

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (this.removeAssetDialogRef === dialogRef) {
          this.removeAssetDialogRef = null;
        }

        if (confirmed !== true) {
          return;
        }

        this.removeAsset(data.packageId, data.assetId);
      });
  }

  getStatusLabel(status: PackageStatus): string {
    return getPackageStatusLabel(status);
  }

  getEventTypeLabel(event: PackageEvent): string {
    return getPackageEventTypeLabel(event.type);
  }

  getEventIcon(event: PackageEvent): string {
    switch (event.type) {
      case 'Created':
        return 'inventory_2';

      case 'AssetAdded':
        return 'add_circle';

      case 'AssetRemoved':
        return 'remove_circle';

      case 'CourierAssigned':
      case 'CourierChanged':
      case 'CourierRemoved':
        return 'local_shipping';

      case 'Dispatched':
        return 'outbound';

      case 'Received':
        return 'move_to_inbox';

      case 'Cancelled':
        return 'cancel';

      default:
        return 'help_outline';
    }
  }

  getEventDescription(event: PackageEvent): string | null {
    switch (event.type) {
      case 'AssetAdded':
        return 'Az eszköz hozzáadva a csomaghoz.';

      case 'AssetRemoved':
        return 'Az eszköz eltávolítva a csomagból.';

      case 'CourierAssigned':
        return 'Futár hozzárendelve a csomaghoz.';

      case 'CourierChanged':
        return 'A csomag futárja módosítva.';

      case 'CourierRemoved':
        return 'A futár eltávolítva a csomagról.';

      default:
        return null;
    }
  }

  toUtcDate(value: string): string {
    // Preserve existing timezone information.
    if (/(?:z|[+-]\d{2}:?\d{2})$/i.test(value)) {
      return value;
    }

    return `${value}Z`;
  }

  private removeAsset(packageId: string, assetId: string): void {
    if (
      this.destroyRef.destroyed ||
      !this.canEditAssets() ||
      this.assetOperationInProgress() ||
      this.packageDetails().id !== packageId
    ) {
      return;
    }

    const exists = this.packageDetails().items.some(item => item.assetId === assetId);

    if (!exists) {
      return;
    }

    this.assetOperationInProgress.set(true);

    this.packagesApi
      .removeAsset(packageId, assetId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.assetOperationInProgress.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.assetsChanged.emit();
        },
        error: (error: unknown) => {
          console.error('Failed to remove package asset.', error);
          this.showError(this.getRemoveAssetErrorMessage(error),
          );
        },
      });
  }

  private getRemoveAssetErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 403:
          return 'Nincs jogosultságod a csomag módosításához.';

        case 404:
          return 'A csomag vagy az eszköz nem található.';

        case 409:
          return 'Az eszköz nem távolítható el. Frissítsd a csomag adatait, és ellenőrizd az aktuális állapotát.';

        case 0:
          return 'A művelet eredménye nem ellenőrizhető. Frissítsd a csomag adatait.';

        default:
          return 'Az eszköz eltávolítása nem sikerült.';
      }
    }

    return 'Az eszköz eltávolítása nem sikerült.';
  }

  private showError(message: string): void {
    if (this.destroyRef.destroyed) {
      return;
    }

    this.snackBar.open(message, 'Bezárás', {
      duration: 7000,
      horizontalPosition: 'end',
      verticalPosition: 'bottom',
      politeness: 'assertive',
      panelClass: [
        'asset-management-snackbar',
        'asset-management-error',
      ],
    });
  }
}