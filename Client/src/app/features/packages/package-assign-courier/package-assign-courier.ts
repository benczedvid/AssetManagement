import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  TemplateRef,
  ViewChild,
  computed,
  inject,
  signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';

import { A11yModule } from '@angular/cdk/a11y';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { ApplicationUsersApi } from '../../users/data-access/application-users-api';
import type { ApplicationUserListItem } from '../../users/models/application-user-list-item';
import { PackagesApi } from '../data-access/packages-api';
import type { AssignCourierRequest } from '../models/assign-courier-request';
import type { AssignCourierResponse } from '../models/assign-courier-response';

interface CourierConfirmationData {
  courierName: string;
  isModification: boolean;
}

@Component({
  selector: 'app-package-assign-courier',
  standalone: true,
  imports: [
    FormsModule,
    A11yModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
  ],
  templateUrl: './package-assign-courier.html',
  styleUrl: './package-assign-courier.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageAssignCourier implements OnInit {
  private static readonly commentMaxLength = 2000;

  private readonly packagesApi = inject(PackagesApi);
  private readonly applicationUsersApi = inject(ApplicationUsersApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  @Input({ required: true })
  packageId!: string;

  @Input()
  currentCourierUserId: string | null = null;

  @Output()
  readonly courierAssigned = new EventEmitter<AssignCourierResponse>();

  @ViewChild('confirmationDialog', { static: true })
  private confirmationDialog!: TemplateRef<{
    $implicit: CourierConfirmationData;
  }>;

  private confirmationDialogRef: MatDialogRef<unknown, boolean> | null = null;
  readonly applicationUsers = signal<readonly ApplicationUserListItem[]>([]);
  readonly selectedCourierUserId = signal<string | null>(null);
  readonly comment = signal('');
  readonly isLoadingUsers = signal(false);
  readonly isSaving = signal(false);
  readonly isConfirming = signal(false);
  readonly loadFailed = signal(false);

  readonly busy = computed(() => this.isLoadingUsers() || this.isSaving() || this.isConfirming());
  readonly selectedCourier = computed(() => this.applicationUsers().find(user => user.id === this.selectedCourierUserId()) ?? null);
  readonly commentLength = computed(() => this.comment().length);

  readonly canSave = computed(
    () =>
      !this.busy() &&
      !this.loadFailed() &&
      this.selectedCourier() !== null &&
      this.commentLength() <= PackageAssignCourier.commentMaxLength,
  );

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.confirmationDialogRef?.close(false);
      this.confirmationDialogRef = null;
    });
  }

  ngOnInit(): void {
    this.selectedCourierUserId.set(this.currentCourierUserId);
    this.loadApplicationUsers();
  }

  updateSelectedCourierUserId(value: string | null): void {
    if (this.busy()) {
      return;
    }

    this.selectedCourierUserId.set(value?.trim() ? value : null);
  }

  updateComment(value: string | null): void {
    if (this.busy()) {
      return;
    }

    this.comment.set(value ?? '');
  }

  retryLoadingUsers(): void {
    if (this.busy()) {
      return;
    }

    this.loadApplicationUsers();
  }

  assignCourier(): void {
    if (this.destroyRef.destroyed || this.busy() || this.loadFailed()) {
      return;
    }

    const validationMessage = this.validate();

    if (validationMessage) {
      this.showError(validationMessage);
      return;
    }

    const courier = this.selectedCourier();

    if (!courier) {
      return;
    }

    // Capture the confirmed values before opening the dialog.
    const packageId = this.packageId;

    const request: AssignCourierRequest = {
      courierUserId: courier.id,
      comment: this.normalizeOptionalText(this.comment()),
    };

    this.isConfirming.set(true);

    const dialogRef = this.dialog.open<unknown, CourierConfirmationData, boolean>(this.confirmationDialog, {
      data: {
        courierName: courier.displayName,
        isModification: !!this.currentCourierUserId,
      },
      width: '440px',
      maxWidth: '94vw',
      ariaLabelledBy: 'courier-confirmation-title',
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

        this.isConfirming.set(false);

        if (confirmed !== true || this.destroyRef.destroyed || this.packageId !== packageId) {
          return;
        }

        this.saveCourier(packageId, request);
      });
  }

  private saveCourier(packageId: string, request: AssignCourierRequest): void {
    if (this.destroyRef.destroyed || this.isSaving() || this.isLoadingUsers() || this.loadFailed()) {
      return;
    }

    const validationMessage = this.validate();

    if (validationMessage) {
      this.showError(validationMessage);
      return;
    }

    this.isSaving.set(true);

    this.packagesApi
      .assignCourier(packageId, request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.isSaving.set(false);
        }),
      )
      .subscribe({
        next: response => {
          this.selectedCourierUserId.set(
            response.courierUserId,
          );

          // The parent closes the editor, refreshes the
          // package and displays the success snackbar.
          this.courierAssigned.emit(response);
        },
        error: (error: unknown) => {
          console.error('Failed to update package courier.', error);
          this.showError(this.getErrorMessage(error, 'save'));
        },
      });
  }

  private loadApplicationUsers(): void {
    if (this.destroyRef.destroyed || this.isLoadingUsers()) {
      return;
    }

    this.isLoadingUsers.set(true);
    this.loadFailed.set(false);
    this.applicationUsers.set([]);

    this.applicationUsersApi
      .getList()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.isLoadingUsers.set(false);
        }),
      )
      .subscribe({
        next: users => {
          this.applicationUsers.set(
            [...users].sort((left, right) =>
              left.displayName.localeCompare(
                right.displayName,
                'hu-HU',
                { sensitivity: 'base' },
              ),
            ),
          );
        },
        error: (error: unknown) => {
          console.error('Failed to load application users.', error);

          this.applicationUsers.set([]);
          this.loadFailed.set(true);

          this.showError(this.getErrorMessage(error, 'load'));
        },
      });
  }

  private validate(): string | null {
    if (!this.packageId?.trim()) {
      return 'A csomag azonosítója hiányzik.';
    }

    if (!this.selectedCourier()) {
      return 'Válassz egy elérhető futárt.';
    }

    if (this.commentLength() > PackageAssignCourier.commentMaxLength) {
      return `A megjegyzés legfeljebb ${PackageAssignCourier.commentMaxLength} karakter lehet.`;
    }

    return null;
  }

  private normalizeOptionalText(value: string): string | null {
    return value.trim() || null;
  }

  private getErrorMessage(error: unknown, operation: 'load' | 'save'): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 0:
          return operation === 'save'
            ? 'A mentés eredménye nem ellenőrizhető. Újabb mentés előtt frissítsd a csomag adatait.'
            : 'A szerver nem érhető el. Ellenőrizd a kapcsolatot, majd próbáld újra.';

        case 401:
          return 'A munkamenet lejárt. Jelentkezz be újra.';

        case 403:
          return 'Nincs jogosultságod a művelet végrehajtásához.';

        case 404:
          return operation === 'save'
            ? 'A csomag vagy a kiválasztott felhasználó nem található.'
            : 'A felhasználók nem tölthetők be.';

        case 400:
        case 422:
          return operation === 'save'
            ? 'A futár a megadott adatokkal nem rendelhető a csomaghoz.'
            : 'A felhasználók betöltése nem sikerült.';

        case 409:
          return operation === 'save'
            ? 'A módosítás ütközik a csomag aktuális állapotával vagy egy üzleti szabállyal. Frissítsd a csomag adatait.'
            : 'A felhasználók betöltése nem sikerült.';
      }
    }

    return operation === 'load'
      ? 'A felhasználók betöltése nem sikerült.'
      : 'A futár módosítása nem sikerült.';
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