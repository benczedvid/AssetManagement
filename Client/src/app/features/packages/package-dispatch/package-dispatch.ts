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
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';

import { A11yModule } from '@angular/cdk/a11y';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize } from 'rxjs';
import { getPackageStatusLabel, PackagesApi } from '../data-access/packages-api';
import type { DispatchPackageRequest, DispatchPackageResponse, PackageDetails as PackageDetailsModel, PackageStatus } from '../data-access/packages-api';

interface DispatchConfirmationData {
  packageNumber: string;
  source: string;
  destination: string;
  courierName: string;
  assetCount: number;
  comment: string | null;
}

@Component({
  selector: 'app-package-dispatch',
  standalone: true,
  imports: [
    FormsModule,
    A11yModule,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './package-dispatch.html',
  styleUrl: './package-dispatch.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageDispatch {
  private static readonly commentMaxLength = 2000;

  private readonly packagesApi = inject(PackagesApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);

  @ViewChild('dispatchConfirmationDialog', { static: true })
  private dispatchConfirmationDialog!: TemplateRef<{
    $implicit: DispatchConfirmationData;
  }>;

  private confirmationDialogRef: MatDialogRef<unknown, boolean> | null = null;

  readonly packageDetails = input.required<PackageDetailsModel>();

  readonly completed = output<DispatchPackageResponse>();
  readonly cancelled = output<void>();
  readonly failed = output<string>();

  readonly comment = signal('');
  readonly submitting = signal(false);
  readonly confirming = signal(false);

  readonly busy = computed(() => this.submitting() || this.confirming());
  readonly commentLength = computed(() => this.comment().length);
  readonly canSubmit = computed(() => !this.busy() && this.validate() === null);

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.confirmationDialogRef?.close(false);
      this.confirmationDialogRef = null;
    });
  }

  updateComment(value: string): void {
    if (this.busy()) {
      return;
    }

    this.comment.set(value ?? '');
  }

  submit(): void {
    if (this.destroyRef.destroyed || this.busy()) {
      return;
    }

    const validationMessage = this.validate();

    if (validationMessage) {
      this.failed.emit(validationMessage);
      return;
    }

    const details = this.packageDetails();
    const packageId = details.id;
    const courierUserId = details.courierUserId;
    const normalizedComment = this.comment().trim();

    const request: DispatchPackageRequest = {
      comment: normalizedComment || null,
    };

    this.confirming.set(true);

    const dialogRef = this.dialog.open<unknown, DispatchConfirmationData, boolean>(this.dispatchConfirmationDialog, {
      data: {
        packageNumber: details.packageNumber,
        source: `${details.sourceStoreNumber} - ` +  details.sourceStoreName,
        destination: `${details.destinationStoreNumber} - ` + details.destinationStoreName,
        courierName: details.courierUserName || 'Nincs megadva',
        assetCount: details.items.length,
        comment: request.comment,
      },
      width: '520px',
      maxWidth: '94vw',
      maxHeight: '90dvh',
      ariaLabelledBy: 'dispatch-package-confirmation-title',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
    });

    this.confirmationDialogRef = dialogRef;

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (this.confirmationDialogRef === dialogRef) {
          this.confirmationDialogRef = null;
        }

        this.confirming.set(false);

        if (confirmed !== true || this.destroyRef.destroyed) {
          return;
        }

        const currentDetails = this.packageDetails();

        if (
          currentDetails.id !== packageId ||
          currentDetails.courierUserId !== courierUserId ||
          currentDetails.sourceStoreId !== details.sourceStoreId ||
          currentDetails.destinationStoreId !==
            details.destinationStoreId ||
          !this.hasSameAssets(details, currentDetails)
        ) {
          this.failed.emit('A csomag adatai időközben megváltoztak. Ellenőrizd az adatokat, és indítsd újra a feladást.');
          return;
        }

        this.dispatchPackage(packageId, request);
      });
  }

  cancel(): void {
    if (this.busy()) {
      return;
    }

    this.comment.set('');
    this.cancelled.emit();
  }

  getStatusLabel(status: PackageStatus): string {
    return getPackageStatusLabel(status);
  }

  private dispatchPackage(packageId: string, request: DispatchPackageRequest): void {
    if (this.destroyRef.destroyed || this.busy() || this.packageDetails().id !== packageId) {
      return;
    }

    const validationMessage = this.validate(request.comment ?? '');

    if (validationMessage) {
      this.failed.emit(validationMessage);
      return;
    }

    this.submitting.set(true);

    this.packagesApi
      .dispatch(packageId, request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.submitting.set(false);
        }),
      )
      .subscribe({
        next: response => {
          this.comment.set('');

          // The parent refreshes the package and displays
          // the success snackbar.
          this.completed.emit(response);
        },
        error: (error: unknown) => {
          console.error('Failed to dispatch package.', error);

          // The parent displays a single error snackbar.
          this.failed.emit(this.getErrorMessage(error));
        },
      });
  }

  private validate(comment: string = this.comment()): string | null {
    const details = this.packageDetails();

    if (details.status !== 'Created') {
      return 'Csak előkészített csomag adható fel.';
    }

    if (!details.courierUserId) {
      return 'A csomag feladása előtt rendelj futárt a csomaghoz.';
    }

    if (comment.length > PackageDispatch.commentMaxLength) {
      return `A megjegyzés legfeljebb ${PackageDispatch.commentMaxLength} karakter lehet.`;
    }

    return null;
  }

  private hasSameAssets(previous: PackageDetailsModel, current: PackageDetailsModel): boolean {
    if (previous.items.length !== current.items.length) {
      return false;
    }

    const previousAssetIds = new Set(previous.items.map(item => item.assetId));

    return current.items.every(item => previousAssetIds.has(item.assetId));
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 0:
          return 'A feladás eredménye nem ellenőrizhető. Újabb feladás előtt frissítsd a csomag adatait.';

        case 401:
          return 'A munkamenet lejárt. Jelentkezz be újra.';

        case 403:
          return 'Nincs jogosultságod a csomag feladásához.';

        case 404:
          return 'A csomag vagy a művelethez szükséges adat nem található. Frissítsd a csomag adatait.';

        case 400:
        case 422:
          return 'A csomag a megadott adatokkal nem adható fel. Ellenőrizd a futárt, az eszközöket és a csomag állapotát.';

        case 409:
          return 'A feladás ütközik a csomag aktuális állapotával vagy egy üzleti szabállyal. Frissítsd a csomag adatait.';
      }
    }

    return 'A csomag feladása nem sikerült.';
  }
}