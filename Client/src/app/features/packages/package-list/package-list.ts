import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  TemplateRef,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';

import { A11yModule } from '@angular/cdk/a11y';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import {
  MatDialog,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  MatSnackBar,
  MatSnackBarModule,
} from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';

import { finalize, firstValueFrom } from 'rxjs';

import { CurrentUserApi } from '../../auth/current-user-api';

import {
  getPackageStatusLabel,
  PackagesApi,
} from '../data-access/packages-api';

import type {
  AssignCourierResponse,
  CancelPackageResponse,
  CreatePackageResponse,
  DispatchPackageResponse,
  GetPackagesResponse,
  PackageDetails as PackageDetailsModel,
  PackageListItem,
  PackageStatus,
  ReceivePackageResponse,
} from '../data-access/packages-api';

import { PackageCancel } from '../package-cancel/package-cancel';
import { PackageCreate } from '../package-create/package-create';
import { PackageDetails } from '../package-details/package-details';
import { PackageDispatch } from '../package-dispatch/package-dispatch';
import { PackageReceive } from '../package-receive/package-receive';

interface PackageConfirmationData {
  title: string;
  message: string;
  confirmLabel: string;
}

type PackageAction =
  | 'dispatch'
  | 'receive'
  | 'cancel'
  | null;

@Component({
  selector: 'app-package-list',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    A11yModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTableModule,
    PackageCreate,
    PackageDetails,
    PackageDispatch,
    PackageReceive,
    PackageCancel,
  ],
  templateUrl: './package-list.html',
  styleUrl: './package-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageList implements OnInit {
  private readonly packagesApi = inject(PackagesApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly materialDialog = inject(MatDialog);
  private readonly packageSnackBar = inject(MatSnackBar);

  readonly currentUserApi = inject(CurrentUserApi);

  @ViewChild('packageDetailsDialog', { static: true })
  private packageDetailsDialog!: TemplateRef<unknown>;

  @ViewChild('confirmationDialog', { static: true })
  private confirmationDialog!: TemplateRef<{
    $implicit: PackageConfirmationData;
  }>;
  @ViewChild('packageCreateDialog', { static: true })
  private packageCreateDialog!: TemplateRef<unknown>;

  private packageCreateDialogRef:
    MatDialogRef<unknown, void> | null = null;

  private packageDetailsDialogRef:
    MatDialogRef<unknown, void> | null = null;

  private confirmationDialogRef:
    MatDialogRef<unknown, boolean> | null = null;

  private detailsRequestSequence = 0;
  private listReloadRequested = false;

  readonly displayedColumns: string[] = [
    'packageNumber',
    'source',
    'destination',
    'assetCount',
    'courier',
    'status',
    'createdAt',
  ];

  readonly items = signal<readonly PackageListItem[]>([]);

  readonly listLoading = signal(false);
  readonly listError = signal<string | null>(null);

  readonly searchTerm = signal('');
  readonly selectedStatuses = signal<readonly PackageStatus[]>([]);

  readonly selectedPackageId = signal<string | null>(null);

  readonly selectedPackage =
    signal<PackageDetailsModel | null>(null);

  readonly detailsLoading = signal(false);
  readonly detailsError = signal<string | null>(null);

  readonly createFormVisible = signal(false);
  readonly activeAction = signal<PackageAction>(null);

  readonly filteredItems = computed(() => {
    const normalizedSearchTerm = this.searchTerm()
      .trim()
      .toLocaleLowerCase('hu-HU');

    const selectedStatuses = new Set(
      this.selectedStatuses(),
    );

    return this.items().filter(item => {
      const statusMatches = selectedStatuses.size === 0 || selectedStatuses.has(item.status);

      if (!statusMatches) {
        return false;
      }

      if (normalizedSearchTerm.length === 0) {
        return true;
      }

      return [
        item.packageNumber,
        item.sourceStoreNumber,
        item.sourceStoreName,
        item.destinationStoreNumber,
        item.destinationStoreName,
        item.courierUserName,
      ].some(value =>
        String(value ?? '')
          .toLocaleLowerCase('hu-HU')
          .includes(normalizedSearchTerm),
      );
    });
  });

  readonly filterCounts = computed(() => {
    const items = this.items();

    return {
      all: items.length,
      created: items.filter(
        item => item.status === 'Created',
      ).length,
      inTransit: items.filter(
        item => item.status === 'InTransit',
      ).length,
      received: items.filter(
        item => item.status === 'Received',
      ).length,
      cancelled: items.filter(
        item => item.status === 'Cancelled',
      ).length,
    };
  });

  readonly selectedListItem = computed(() => {
    const selectedId = this.selectedPackageId();

    if (!selectedId) {
      return null;
    }

    return (this.items().find(item => item.id === selectedId) ?? null);
  });
  readonly hasSelectedPackage = computed(
    () =>
      this.selectedPackageId() !== null &&
      this.selectedPackage() !== null,
  );
  readonly actionVisible = computed(() => this.activeAction() !== null);
  readonly canViewPackages = computed(() => this.currentUserApi.canViewPackages());
  readonly canManagePackages = computed(() => this.currentUserApi.canManagePackages());
  readonly canCreatePackage = computed(() => this.canManagePackages());

  readonly canAssignCourier = computed(() => {
    const packageDetails = this.selectedPackage();

    if (!packageDetails || packageDetails.status !== 'Created') {
      return false;
    }

    return this.canManagePackageAtSource(packageDetails.sourceStoreId);
  });

  readonly canDispatchPackage = computed(() => {
    const packageDetails = this.selectedPackage();

    if (!packageDetails || packageDetails.status !== 'Created') {
      return false;
    }

    return this.canManagePackageAtSource(packageDetails.sourceStoreId);
  });

  readonly canCancelPackage = computed(() => {
    const packageDetails = this.selectedPackage();

    if (!packageDetails || packageDetails.status !== 'Created') {
      return false;
    }

    return this.canManagePackageAtSource(packageDetails.sourceStoreId);
  });

  readonly canReceivePackage = computed(() => {
    const packageDetails = this.selectedPackage();

    if (!packageDetails || packageDetails.status !== 'InTransit') {
      return false;
    }

    if (this.currentUserApi.canManageAllPackages()) {
      return true;
    }

    return (
      this.currentUserApi.canManageOwnStorePackages() &&
      this.currentUserApi.storeId() === packageDetails.destinationStoreId);
  });

  readonly canManagePackageAssets = computed(() => {
    const packageDetails = this.selectedPackage();

    if (!packageDetails || packageDetails.status !== 'Created') {
      return false;
    }

    return this.canManagePackageAtSource(packageDetails.sourceStoreId);
  });

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.detailsRequestSequence++;
      this.listReloadRequested = false;

      this.confirmationDialogRef?.close(false);
      this.packageDetailsDialogRef?.close();

      this.confirmationDialogRef = null;
      this.packageDetailsDialogRef = null;

      this.packageCreateDialogRef?.close();
    });
  }

  ngOnInit(): void {
    this.currentUserApi
      .getOrCreateCurrentUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          if (this.canViewPackages()) {
            this.loadList();
            return;
          }

          const message = 'You do not have permission to view packages.';

          this.listError.set(message);
          this.showError(message);
        },
        error: error => {
          console.error('Failed to load current user.', error);

          const message = this.getErrorMessage(error, 'Failed to load the current user.');

          this.listError.set(message);
          this.showError(message);
        },
      });
  }

  loadList(): void {
    if (this.destroyRef.destroyed) {
      return;
    }

    if (!this.canViewPackages()) {
      const message = 'Nincs jogosultságod csomagok megtekintésére.';

      this.listError.set(message);
      this.showError(message);
      return;
    }

    if (this.listLoading()) {
      this.listReloadRequested = true;
      return;
    }

    this.listReloadRequested = false;
    this.listLoading.set(true);
    this.listError.set(null);

    this.packagesApi
      .getList()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.listLoading.set(false);

          if (this.listReloadRequested && !this.destroyRef.destroyed) {
            this.listReloadRequested = false;
            this.loadList();
          }
        }),
      )
      .subscribe({
        next: (response: GetPackagesResponse) => {
          this.items.set(response.items);

          // A queued request will fetch the latest state.
          if (this.listReloadRequested) {
            return;
          }

          const selectedId = this.selectedPackageId();

          if (!selectedId) {
            return;
          }

          const selectedPackageStillVisible =
            response.items.some(item => item.id === selectedId);

          if (!selectedPackageStillVisible) {
            this.clearSelection();
            return;
          }

          this.loadDetails(selectedId);
        },
        error: error => {
          console.error('Failed to load packages.', error);

          const message = this.getErrorMessage(error, 'Failed to load packages.');

          this.items.set([]);

          // Keep an open details dialog independent of
          // a temporary list-loading failure.
          this.listError.set(message);
          this.showError(message);
        },
      });
  }

  loadDetails(id: string): void {
    if (this.destroyRef.destroyed || this.selectedPackageId() !== id) {
      return;
    }

    const requestSequence = ++this.detailsRequestSequence;

    this.detailsLoading.set(true);
    this.detailsError.set(null);

    this.packagesApi
      .getDetails(id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          if (requestSequence === this.detailsRequestSequence) {
            this.detailsLoading.set(false);
          }
        }),
      )
      .subscribe({
        next: details => {
          if (requestSequence !==  this.detailsRequestSequence || this.selectedPackageId() !== id) {
            return;
          }

          this.selectedPackage.set(details);
        },
        error: error => {
          if (requestSequence !== this.detailsRequestSequence || this.selectedPackageId() !== id) {
            return;
          }

          console.error('Failed to load package details.', error);

          const message = this.getErrorMessage(error, 'Failed to load package details.');

          this.selectedPackage.set(null);
          this.detailsError.set(message);
          this.showError(message);
        },
      });
  }

  selectPackage(id: string): void {
    if (!this.canViewPackages()) {
      this.showError('Nincs jogosultságod csomag megtekintésére.');
      return;
    }

    if (
      this.selectedPackageId() === id &&
      this.selectedPackage() &&
      !this.detailsError()
    ) {
      return;
    }

    this.selectedPackageId.set(id);
    this.selectedPackage.set(null);
    this.detailsError.set(null);
    this.activeAction.set(null);

    this.loadDetails(id);
  }

  clearSelection(): void {
    this.resetSelectionState();
    this.closePackageDialog();
  }

  retryDetails(): void {
    const selectedId = this.selectedPackageId();

    if (!selectedId || this.detailsLoading()) {
      return;
    }

    this.loadDetails(selectedId);
  }

  updateSearchTerm(value: string): void {
    this.searchTerm.set(value ?? '');
  }

  selectStatuses(values: readonly PackageStatus[] | null | undefined): void {
    const validStatuses = (values ?? []).filter(status =>
      status === 'Created' ||
      status === 'InTransit' ||
      status === 'Received' ||
      status === 'Cancelled' ||
      status === 'Unknown',
    );

    this.selectedStatuses.set(
      [...new Set(validStatuses)],
    );
  }

  clearStatusFilters(): void {
    this.selectedStatuses.set([]);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedStatuses.set([]);
  }

  openPackageDialog(packageId: string): void {
    if (this.destroyRef.destroyed || this.packageDetailsDialogRef) {
      return;
    }

    if (!this.canViewPackages()) {
      this.showError('Nincs jogosultságod csomag megtekintésére.');
      return;
    }

    this.closeCreateForm();
    this.closeAction();
    this.selectPackage(packageId);

    const dialogRef = this.materialDialog.open<unknown, unknown, void>(
        this.packageDetailsDialog,
        {
          width: '1100px',
          maxWidth: '96vw',
          maxHeight: '92dvh',
          ariaLabelledBy: 'package-details-title',
          autoFocus: 'first-tabbable',
          restoreFocus: true,
        },
      );

    this.packageDetailsDialogRef = dialogRef;

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.packageDetailsDialogRef !== dialogRef) {
          return;
        }

        this.packageDetailsDialogRef = null;
        this.resetSelectionState();
      });
  }

  closePackageDialog(): void {
    this.confirmationDialogRef?.close(false);
    this.packageDetailsDialogRef?.close();
  }

  openCreateForm(): void {
    if (!this.canCreatePackage()) {
      this.showError('Nincs jogosultságod csomag létrehozására.');
      return;
    }

    if (this.packageCreateDialogRef) {
      return;
    }

    this.closeAction();

    const dialogRef = this.materialDialog.open<unknown,  unknown,void>(this.packageCreateDialog, {
      width: '900px',
      maxWidth: '96vw',
      maxHeight: '92dvh',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
    });

    this.packageCreateDialogRef = dialogRef;
    this.createFormVisible.set(true);

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.packageCreateDialogRef === dialogRef) {
          this.packageCreateDialogRef = null;
        }

        this.createFormVisible.set(false);
      });
  }

  closeCreateForm(): void {
    this.packageCreateDialogRef?.close();
    this.createFormVisible.set(false);
  }

  handlePackageCreated(response: CreatePackageResponse): void {
    this.closeCreateForm();
    this.activeAction.set(null);
    this.showSuccess(`A(z) ${response.packageNumber} csomag sikeresen létrehozva.`);
    this.openPackageDialog(response.id);
    this.loadList();
  }

  handlePackageAssetsChanged(): void {
    const packageDetails = this.selectedPackage();

    if (!packageDetails) {
      return;
    }

    this.handleActionCompleted(packageDetails.id, `A(z) ${packageDetails.packageNumber} csomag eszközei sikeresen módosultak.`);
  }

  openDispatch(): void {
    if (!this.canDispatchPackage()) {
      return;
    }

    this.openAction('dispatch');
  }

  openReceive(): void {
    if (!this.canReceivePackage()) {
      return;
    }

    this.openAction('receive');
  }

  openCancel(): void {
    if (!this.canCancelPackage()) {
      return;
    }

    this.openAction('cancel');
  }

  closeAction(): void {
    this.activeAction.set(null);
  }

  handleCourierAssigned(response: AssignCourierResponse): void {
    const message = response.courierUserId
      ? `A(z) ${response.packageNumber} csomaghoz rendeltek egy futárt.`
      : `A(z) ${response.packageNumber} csomaghoz nem rendeltek futárt.`;

    this.handleActionCompleted(response.packageId, message);
  }

  handlePackageDispatched(response: DispatchPackageResponse): void {
    this.handleActionCompleted(response.packageId,
      `A(z) ${response.packageNumber} csomag sikeresen átadva kiszállításra.`
    );
  }

  handlePackageReceived(response: ReceivePackageResponse): void {
    this.handleActionCompleted(response.packageId,
      `A(z) ${response.packageNumber} csomag sikeresen átvéve.`
    );
  }

  handlePackageCancelled(response: CancelPackageResponse): void {
    this.handleActionCompleted(response.packageId,
      `A(z) ${response.packageNumber} csomag sikeresen törölve.`
    );
  }

  handleActionError(message: string): void {
    this.showError(message?.trim() || 'Hiba a művelet végrehajtása során');
  }

  getStatusLabel(status: PackageStatus): string {
    return getPackageStatusLabel(status);
  }

  async confirmPackageAction(data: PackageConfirmationData): Promise<boolean> {
    if (this.destroyRef.destroyed || this.confirmationDialogRef
    ) {
      return false;
    }

    const dialogRef = this.materialDialog.open<unknown, PackageConfirmationData, boolean >(this.confirmationDialog, {
      data,
      width: '440px',
      maxWidth: '94vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
    });

    this.confirmationDialogRef = dialogRef;

    try {
      const confirmed = await firstValueFrom(
        dialogRef
          .afterClosed()
          .pipe(takeUntilDestroyed(this.destroyRef)),
        { defaultValue: false },
      );

      return (
        confirmed === true &&  !this.destroyRef.destroyed
      );
    } finally {
      if (this.confirmationDialogRef === dialogRef) {
        this.confirmationDialogRef = null;
      }
    }
  }

  private openAction(action: Exclude<PackageAction, null>): void {
    if (this.detailsLoading() || !this.selectedPackage()) {
      return;
    }

    this.createFormVisible.set(false);
    this.activeAction.set(action);
  }

  private handleActionCompleted(packageId: string, message: string): void {
    this.showSuccess(message);

    if (this.selectedPackageId() === packageId) {
      this.activeAction.set(null);
      this.selectedPackage.set(null);
      this.loadDetails(packageId);
    }

    this.loadList();
  }

  private resetSelectionState(): void {
    this.detailsRequestSequence++;

    this.selectedPackageId.set(null);
    this.selectedPackage.set(null);
    this.detailsLoading.set(false);
    this.detailsError.set(null);
    this.activeAction.set(null);
  }

  private canManagePackageAtSource(sourceStoreId: string): boolean {
    if (this.currentUserApi.canManageAllPackages()) {
      return true;
    }

    return (
      this.currentUserApi.canManageOwnStorePackages() &&
      this.currentUserApi.storeId() === sourceStoreId
    );
  }

  private showSuccess(message: string): void {
    if (this.destroyRef.destroyed) {
      return;
    }

    this.packageSnackBar.open(message, 'Bezárás', {
      duration: 4000,
      horizontalPosition: 'end',
      verticalPosition: 'bottom',
      panelClass: [
        'asset-management-snackbar',
        'asset-management-success',
      ],
    });
  }

  private showError(message: string): void {
    if (this.destroyRef.destroyed) {
      return;
    }

  this.packageSnackBar.open(message, 'Bezárás', {
    duration: 7000,
    horizontalPosition: 'end',
    verticalPosition: 'bottom',
    panelClass: [
      'asset-management-snackbar',
      'asset-management-error',
    ],
  });
}

  private getErrorMessage(error: unknown, fallbackMessage: string): string {
    if (typeof error !== 'object' || error === null || !('error' in error)) {
      return fallbackMessage;
    }

    const responseError = (
      error as { error?: unknown }
    ).error;

    if (typeof responseError !== 'object' || responseError === null) {
      return fallbackMessage;
    }

    if ('detail' in responseError && typeof responseError.detail === 'string' && responseError.detail.trim().length > 0) {
      return responseError.detail.trim();
    }

    if ('title' in responseError && typeof responseError.title === 'string' && responseError.title.trim().length > 0) {
      return responseError.title.trim();
    }

    return fallbackMessage;
  }
}