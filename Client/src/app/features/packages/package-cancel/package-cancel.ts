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
import type { CancelPackageRequest, CancelPackageResponse, PackageDetails as PackageDetailsModel, PackageStatus } from '../data-access/packages-api';

interface CancelConfirmationData {
  packageNumber: string;
  comment: string;
}

@Component({
  selector: 'app-package-cancel',
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
  templateUrl: './package-cancel.html',
  styleUrl: './package-cancel.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageCancel {
  private static readonly commentMaxLength = 2000;

  private readonly packagesApi = inject(PackagesApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);

  @ViewChild('cancelConfirmationDialog', { static: true })
  private cancelConfirmationDialog!: TemplateRef<{
    $implicit: CancelConfirmationData;
  }>;

  private confirmationDialogRef: MatDialogRef<unknown, boolean> | null = null;
  readonly packageDetails = input.required<PackageDetailsModel>();

  readonly completed = output<CancelPackageResponse>();
  readonly cancelled = output<void>();
  readonly failed = output<string>();

  readonly comment = signal('');
  readonly submitting = signal(false);
  readonly confirming = signal(false);

  readonly busy = computed(() => this.submitting() || this.confirming());
  readonly commentLength = computed(() => this.comment().length);

  readonly canSubmit = computed(
    () =>
      !this.busy() &&
      this.packageDetails().status === 'Created' &&
      this.comment().trim().length > 0 &&
      this.commentLength() <= PackageCancel.commentMaxLength,
  );

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

    // Capture the exact values shown in the confirmation.
    const packageId = details.id;
    const normalizedComment = this.comment().trim();

    this.confirming.set(true);

    const dialogRef = this.dialog.open<unknown, CancelConfirmationData, boolean>(this.cancelConfirmationDialog, {
      data: {
        packageNumber: details.packageNumber,
        comment: normalizedComment,
      },
      width: '500px',
      maxWidth: '94vw',
      maxHeight: '90dvh',
      ariaLabelledBy: 'cancel-package-confirmation-title',
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

        if (this.packageDetails().id !== packageId) {
          this.failed.emit('A kiválasztott csomag időközben megváltozott. Indítsd újra a műveletet.');
          return;
        }

        this.cancelPackage(packageId, normalizedComment);
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

  private cancelPackage(packageId: string, confirmedComment: string): void {
    if (this.destroyRef.destroyed || this.busy() || this.packageDetails().id !== packageId) {
      return;
    }

    const validationMessage = this.validate(
      confirmedComment,
    );

    if (validationMessage) {
      this.failed.emit(validationMessage);
      return;
    }

    const request: CancelPackageRequest = {
      comment: confirmedComment,
    };

    this.submitting.set(true);

    this.packagesApi
      .cancel(packageId, request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.submitting.set(false);
        }),
      )
      .subscribe({
        next: response => {
          this.comment.set('');

          // The parent refreshes the package and shows
          // the success snackbar.
          this.completed.emit(response);
        },
        error: (error: unknown) => {
          console.error('Failed to cancel package.', error);

          // The parent displays a single error snackbar.
          this.failed.emit(this.getErrorMessage(error));
        },
      });
  }

  private validate(comment: string = this.comment()): string | null {
    if (this.packageDetails().status !== 'Created') {
      return 'Csak előkészített csomag szakítható meg.';
    }

    if (comment.trim().length === 0) {
      return 'A megszakítás indoklása kötelező.';
    }

    if (comment.length > PackageCancel.commentMaxLength) {
      return `Az indoklás legfeljebb ${PackageCancel.commentMaxLength} karakter lehet.`;
    }

    return null;
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 0:
          return 'A művelet eredménye nem ellenőrizhető. Újabb megszakítás előtt frissítsd a csomag adatait.';

        case 401:
          return 'A munkamenet lejárt. Jelentkezz be újra.';

        case 403:
          return 'Nincs jogosultságod a csomag megszakításához.';

        case 404:
          return 'A csomag nem található. Frissítsd a csomaglistát.';

        case 400:
        case 422:
          return 'A csomag a megadott adatokkal nem szakítható meg. Ellenőrizd az indoklást és a csomag állapotát.';

        case 409:
          return 'A megszakítás ütközik a csomag aktuális állapotával vagy egy üzleti szabállyal. Frissítsd a csomag adatait.';
      }
    }

    return 'A csomag megszakítása nem sikerült.';
  }
}