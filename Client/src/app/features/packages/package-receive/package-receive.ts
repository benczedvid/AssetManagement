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
import type { PackageDetails as PackageDetailsModel, PackageStatus, ReceivePackageRequest, ReceivePackageResponse } from '../data-access/packages-api';

interface ReceiveConfirmationData {
  packageNumber: string;
  destination: string;
  assetCount: number;
  comment: string | null;
}

@Component({
  selector: 'app-package-receive',
  standalone: true,
  imports: [
    DatePipe,
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
  templateUrl: './package-receive.html',
  styleUrl: './package-receive.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageReceive {
  private static readonly commentMaxLength = 2000;

  private readonly packagesApi = inject(PackagesApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);

  @ViewChild('receiveConfirmationDialog', { static: true })
  private receiveConfirmationDialog!: TemplateRef<{
    $implicit: ReceiveConfirmationData;
  }>;

  private confirmationDialogRef: MatDialogRef<unknown, boolean> | null = null;
  readonly packageDetails = input.required<PackageDetailsModel>();
  readonly completed = output<ReceivePackageResponse>();
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

    // Capture the values being confirmed.
    const packageId = details.id;
    const destinationStoreId = details.destinationStoreId;

    const assetIds = new Set(details.items.map(item => item.assetId));
    const normalizedComment = this.comment().trim();
    const request: ReceivePackageRequest = {
      receivingStoreId: destinationStoreId,
      comment: normalizedComment || null,
    };

    this.confirming.set(true);

    const dialogRef = this.dialog.open<unknown, ReceiveConfirmationData, boolean>(this.receiveConfirmationDialog, {
      data: {
        packageNumber: details.packageNumber,
        destination: `${details.destinationStoreNumber} - ` + details.destinationStoreName,
        assetCount: details.items.length,
        comment: request.comment,
      },
      width: '500px',
      maxWidth: '94vw',
      maxHeight: '90dvh',
      ariaLabelledBy: 'receive-package-confirmation-title',
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

        const assetsUnchanged =
          currentDetails.items.length === details.items.length &&
          currentDetails.items.every(item => assetIds.has(item.assetId));

        if (
          currentDetails.id !== packageId ||
          currentDetails.destinationStoreId !==
          destinationStoreId ||
          !assetsUnchanged
        ) {
          this.failed.emit('A csomag adatai időközben megváltoztak. Ellenőrizd az adatokat, és indítsd újra az érkeztetést.');
          return;
        }

        this.receivePackage(packageId, request);
      });
  }

  close(): void {
    if (this.busy()) {
      return;
    }

    this.comment.set('');
    this.cancelled.emit();
  }

  getStatusLabel(status: PackageStatus): string {
    return getPackageStatusLabel(status);
  }

  toUtcDate(value: string): string {
    // Preserve existing timezone information.
    if (/(?:z|[+-]\d{2}:?\d{2})$/i.test(value)) {
      return value;
    }

    return `${value}Z`;
  }

  private receivePackage(packageId: string, request: ReceivePackageRequest): void {
    if (this.destroyRef.destroyed || this.busy() || this.packageDetails().id !== packageId) {
      return;
    }

    const validationMessage = this.validate(request.comment ?? '');

    if (validationMessage) {
      this.failed.emit(validationMessage);
      return;
    }

    if (this.packageDetails().destinationStoreId !== request.receivingStoreId) {
      this.failed.emit('A csomag céltelephelye időközben megváltozott. Indítsd újra az érkeztetést.');
      return;
    }

    this.submitting.set(true);

    this.packagesApi
      .receive(packageId, request)
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
          console.error('Failed to receive package.', error);

          // The parent displays a single error snackbar.
          this.failed.emit(this.getErrorMessage(error));
        },
      });
  }

  private validate(comment: string = this.comment()): string | null {
    const details = this.packageDetails();

    if (details.status !== 'InTransit') {
      return 'Csak úton lévő csomag érkeztethető.';
    }

    if (!details.destinationStoreId?.trim()) {
      return 'A csomag céltelephelye nem érvényes.';
    }

    if (comment.length > PackageReceive.commentMaxLength) {
      return `A megjegyzés legfeljebb ${PackageReceive.commentMaxLength} karakter lehet.`;
    }

    return null;
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 0:
          return 'Az érkeztetés eredménye nem ellenőrizhető. Újabb érkeztetés előtt frissítsd a csomag adatait.';

        case 401:
          return 'A munkamenet lejárt. Jelentkezz be újra.';

        case 403:
          return 'Nincs jogosultságod a csomag érkeztetéséhez.';

        case 404:
          return 'A csomag vagy a céltelephely nem található. Frissítsd a csomag adatait.';

        case 400:
        case 422:
          return 'A csomag a megadott adatokkal nem érkeztethető. Ellenőrizd a céltelephelyet és a csomag állapotát.';

        case 409:
          return 'Az érkeztetés ütközik a csomag aktuális állapotával vagy egy üzleti szabállyal. Frissítsd a csomag adatait.';
      }
    }

    return 'A csomag érkeztetése nem sikerült.';
  }
}