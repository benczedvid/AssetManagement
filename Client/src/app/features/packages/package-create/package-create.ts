import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  TemplateRef,
  ViewChild,
  computed,
  inject,
  output,
  signal } from '@angular/core';

import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { PackagesApi } from '../data-access/packages-api';
import type { CreatePackageRequest, CreatePackageResponse } from '../data-access/packages-api';
import { StoreApiService } from '../../stores/data-access/store-api';
import type { GetStoresResponse } from '../../stores/data-access/models/get-stores-response';
import { AssetApi } from '../../assets/data-access/asset-api';
import type { GetAssetsResponse } from '../../assets/models/get-assets-response';
import { ApplicationUsersApi } from '../../users/data-access/application-users-api';
import type { ApplicationUserListItem } from '../../users/models/application-user-list-item';
import { MatIconModule } from '@angular/material/icon';

interface ConfirmationDialogData {
  readonly title: string;
  readonly message: string;
  readonly confirmLabel: string;
  readonly cancelLabel: string;
}

interface ConfirmationDialogContext {
  readonly $implicit: ConfirmationDialogData;
}

@Component({
  selector: 'app-package-create',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatDialogModule,
    MatDividerModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTableModule,
    MatIconModule
  ],
  templateUrl: './package-create.html',
  styleUrl: './package-create.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageCreate {
  private static readonly headquartersStoreNumber = '999';
  private static readonly commentMaxLength = 2000;
  private readonly packagesApi = inject(PackagesApi);
  private readonly storeApi = inject(StoreApiService);
  private readonly assetApi = inject(AssetApi);
  private readonly applicationUsersApi = inject(ApplicationUsersApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  @ViewChild('confirmationDialog', { static: true })
  private confirmationDialogTemplate!: TemplateRef<ConfirmationDialogContext>;
  private activeDialogRef: MatDialogRef<unknown, boolean> | null = null;
  readonly packageCreated = output<CreatePackageResponse>();
  readonly cancelled = output<void>();

  readonly displayedColumns: string[] = [
    'select',
    'asset',
    'serialNumber',
    'status',
  ];

  readonly stores = signal<readonly GetStoresResponse[]>([]);
  readonly assets = signal<readonly GetAssetsResponse[]>([]);
  readonly applicationUsers =  signal<readonly ApplicationUserListItem[]>([]);
  readonly sourceStoreId = signal('');
  readonly destinationStoreId = signal('');
  readonly selectedAssetIds = signal<readonly string[]>([]);
  readonly courierUserId = signal<string | null>(null);
  readonly comment = signal('');
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly confirming = signal(false);
  readonly loadFailed = signal(false);
  readonly busy = computed(() => this.submitting() || this.confirming());
  readonly selectedSourceStore = computed(() => {
    return this.stores().find(store => store.id === this.sourceStoreId()) ?? null;
  });

  readonly selectedDestinationStore = computed(() => {
    return this.stores().find(store => store.id === this.destinationStoreId()) ?? null;
  });

  readonly availableDestinationStores = computed(() => {
    const sourceStore = this.selectedSourceStore();

    if (!sourceStore) {
      return [];
    }

    const sourceIsHeadquarters = this.isHeadquarters(sourceStore);

    return this.stores().filter(store => {
      return (store.id !== sourceStore.id && this.isHeadquarters(store) !== sourceIsHeadquarters);
    });
  });

  readonly availableAssets = computed(() => {
    const sourceStore = this.selectedSourceStore();
    const destinationStore = this.selectedDestinationStore();

    if (!sourceStore || !destinationStore || !this.isValidRoute(sourceStore, destinationStore)) {
      return [];
    }

    return this.assets()
      .filter(asset =>
        this.isAssetAvailableForSelectedRoute(asset, sourceStore, destinationStore),
      )
      .sort((left, right) =>
        left.assetName.localeCompare(
          right.assetName,
          'hu-HU',
          {
            sensitivity: 'base',
          },
        ),
      );
  });
  readonly assetSearchText = signal('');

  readonly filteredAssets = computed(() => {
    const search = this.normalizeAssetSearchText(
      this.assetSearchText(),
    );

    if (!search) {
      return this.availableAssets();
    }

    return this.availableAssets().filter(asset => {
      const values = [
        asset.assetName,
        asset.manufacturer,
        asset.model,
        asset.serialNumber,
        asset.assignedStoreNumber,
      ];

      return values.some(value => this.normalizeAssetSearchText(String(value ?? '')).includes(search));
    });
  });

  readonly allFilteredAssetsSelected = computed(() => {
    const filtered = this.filteredAssets();
    const selectedIds = new Set(this.selectedAssetIds());

    return (filtered.length > 0 && filtered.every(asset => selectedIds.has(asset.assetId)));
  });

  readonly someFilteredAssetsSelected = computed(() => {
    const selectedIds = new Set(this.selectedAssetIds());

    return (
      this.filteredAssets().some(asset =>
        selectedIds.has(asset.assetId),
      ) &&
      !this.allFilteredAssetsSelected()
    );
  });
  readonly selectedAssets = computed(() => {
    const selectedIds = new Set(this.selectedAssetIds());

    return this.availableAssets().filter(asset => selectedIds.has(asset.assetId));
  });

  readonly commentLength = computed(() => this.comment().length);

  readonly hasChanges = computed(() => {
    return (
      this.sourceStoreId().length > 0 ||
      this.destinationStoreId().length > 0 ||
      this.selectedAssetIds().length > 0 ||
      this.courierUserId() !== null ||
      this.comment().length > 0
    );
  });

  readonly canSubmit = computed(() => {
    const sourceStore = this.selectedSourceStore();
    const destinationStore = this.selectedDestinationStore();

    return (
      !this.loading() &&
      !this.loadFailed() &&
      !this.busy() &&
      sourceStore !== null &&
      destinationStore !== null &&
      this.isValidRoute(sourceStore, destinationStore) &&
      this.selectedAssets().length > 0 &&
      this.selectedAssets().length ===
      this.selectedAssetIds().length &&
      this.commentLength() <= PackageCreate.commentMaxLength
    );
  });

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.activeDialogRef?.close(false);
    });

    this.loadFormData();
  }

  updateSourceStore(value: string): void {
    if (this.busy()) {
      return;
    }

    this.sourceStoreId.set(value);
    this.destinationStoreId.set('');
    this.selectedAssetIds.set([]);
  }

  updateDestinationStore(value: string): void {
    if (this.busy()) {
      return;
    }

    this.destinationStoreId.set(value);
    this.selectedAssetIds.set([]);
  }

  updateCourierUser(value: string | null): void {
    if (this.busy()) {
      return;
    }

    this.courierUserId.set(value && value.trim().length > 0 ? value : null);
  }

  updateComment(value: string): void {
    if (this.busy()) {
      return;
    }

    this.comment.set(value);
  }

  toggleAsset(assetId: string, selected: boolean): void {
    if (this.busy()) {
      return;
    }

    const assetIsAvailable = this.availableAssets().some(asset => asset.assetId === assetId);

    if (!assetIsAvailable) {
      this.showError('Ez az eszköz nem választható ki a megadott útvonalhoz.');

      return;
    }

    const selectedIds = new Set(this.selectedAssetIds());

    if (selected) {
      selectedIds.add(assetId);
    } else {
      selectedIds.delete(assetId);
    }

    this.selectedAssetIds.set([...selectedIds]);
  }

  submit(): void {
    if (
      this.loading() ||
      this.loadFailed() ||
      this.busy()
    ) {
      return;
    }

    const validationError = this.validate();

    if (validationError) {
      this.showError(validationError);
      return;
    }

    const sourceStore = this.selectedSourceStore();
    const destinationStore = this.selectedDestinationStore();

    if (!sourceStore || !destinationStore) {
      return;
    }

    this.openConfirmation(
      {
        title: 'Csomag létrehozása',
        message: [
          'Létrehozod a csomagot az alábbi adatokkal?',
          '',
          `Feladó: ${this.getStoreLabel(sourceStore)}`,
          `Címzett: ${this.getStoreLabel(destinationStore)}`,
          `Eszközök száma: ${this.selectedAssets().length}`,
          '',
          'A csomag létrehozása még nem jelenti annak feladását.',
        ].join('\n'),
        confirmLabel: 'Létrehozás',
        cancelLabel: 'Mégse',
      },
      () => this.createPackage(),
    );
  }

  close(): void {
    if (this.busy()) {
      return;
    }

    if (!this.hasChanges()) {
      this.cancelled.emit();
      return;
    }

    this.openConfirmation(
      {
        title: 'Módosítások elvetése',
        message: 'Biztosan bezárod az űrlapot? A megadott adatok és a kiválasztott eszközök nem kerülnek mentésre.',
        confirmLabel: 'Elvetés és bezárás',
        cancelLabel: 'Vissza az űrlaphoz',
      },
      () => this.cancelled.emit(),
    );
  }

  retryLoading(): void {
    if (this.loading() || this.busy()) {
      return;
    }

    this.loadFormData();
  }

  isAssetSelected(assetId: string): boolean {
    return this.selectedAssetIds().includes(assetId);
  }

  getStoreLabel(store: GetStoresResponse): string {
    return `${store.storeNumber} - ${store.name}`;
  }

  getAssetLabel(asset: GetAssetsResponse): string {
    return [
      asset.assetName,
      asset.manufacturer,
      asset.model,
      asset.serialNumber,
    ]
      .filter(value => value.trim().length > 0)
      .join(' | ');
  }

  getAssetStatusLabel(status: string): string {
    switch (status) {
      case 'In_HQ':
        return 'Központban';

      case 'In_Store':
        return 'Áruházban';

      case 'Used_In_HQ':
        return 'Használatban a központban';

      case 'Used_In_Store':
        return 'Használatban az áruházban';

      case 'Route_To_Store':
        return 'Úton az áruházba';

      case 'Route_To_HQ':
        return 'Úton a központba';

      case 'Route_To_Service':
        return 'Úton a szervizbe';

      case 'In_Service':
        return 'Szervizben';

      case 'Disposed':
        return 'Selejtezett';

      default:
        return 'Egyéb állapot';
    }
  }

  updateAssetSearchText(value: string): void {
    if (this.busy() || this.loading()) {
      return;
    }

    this.assetSearchText.set(value ?? '');
  }

  toggleAllFilteredAssets(selected: boolean): void {
    if (this.busy() || this.loading() || this.loadFailed()) {
      return;
    }

    const selectedIds = new Set(this.selectedAssetIds());

    for (const asset of this.filteredAssets()) {
      if (selected) {
        selectedIds.add(asset.assetId);
      } else {
        selectedIds.delete(asset.assetId);
      }
    }

    this.selectedAssetIds.set([...selectedIds]);
  }

  clearAssetSelection(): void {
    if (this.busy()) {
      return;
    }

    this.selectedAssetIds.set([]);
  }

  private normalizeAssetSearchText(value: string): string {
    return value
      .normalize('NFD')
      .replace(/\p{Diacritic}/gu, '')
      .trim()
      .toLocaleLowerCase('hu-HU');
  }

  private openConfirmation(data: ConfirmationDialogData, onConfirmed: () => void): void {
    if (this.busy()) {
      return;
    }

    this.confirming.set(true);

    const dialogRef = this.dialog.open<unknown, ConfirmationDialogData, boolean>(
      this.confirmationDialogTemplate,
      {
        data,
        width: '480px',
        maxWidth: 'calc(100vw - 32px)',
        autoFocus: 'button',
        restoreFocus: true,
        ariaLabel: data.title,
      },
    );

    this.activeDialogRef = dialogRef;

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        this.activeDialogRef = null;
        this.confirming.set(false);

        if (confirmed === true) {
          onConfirmed();
        }
      });
  }

  private createPackage(): void {
    if (this.loading() || this.loadFailed() || this.busy()) {
      return;
    }

    const validationError = this.validate();

    if (validationError) {
      this.showError(validationError);
      return;
    }

    const request: CreatePackageRequest = {
      sourceStoreId: this.sourceStoreId(),
      destinationStoreId: this.destinationStoreId(),
      assetIds: [...this.selectedAssetIds()],
      courierUserId: this.courierUserId(),
      comment: this.normalizeOptionalText(this.comment()),
    };

    this.submitting.set(true);

    this.packagesApi
      .create(request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.submitting.set(false);
        }),
      )
      .subscribe({
        next: response => {
          this.snackBar.open(
            'A csomag sikeresen létrejött.',
            'Bezárás',
            {
              duration: 4000,
              horizontalPosition: 'right',
              verticalPosition: 'bottom',
              politeness: 'polite',
            },
          );

          this.packageCreated.emit(response);
        },
        error: (error: unknown) => {
          console.error('Failed to create package.', error);

          this.showError(
            this.getErrorMessage(error,  'A csomag létrehozása nem sikerült. Próbáld újra.'),
          );
        },
      });
  }

  private loadFormData(): void {
    this.loading.set(true);
    this.loadFailed.set(false);

    forkJoin({
      stores: this.storeApi.getAll(),
      assets: this.assetApi.getAll(),
      users: this.applicationUsersApi.getList(),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.loading.set(false);
        }),
      )
      .subscribe({
        next: response => {
          this.stores.set(
            [...response.stores].sort(
              (left, right) =>
                left.storeNumber.localeCompare(
                  right.storeNumber,
                  'hu-HU',
                  {
                    numeric: true,
                    sensitivity: 'base',
                  },
                ),
            ),
          );

          this.assets.set(response.assets);

          this.applicationUsers.set(
            [...response.users].sort(
              (left, right) =>
                left.displayName.localeCompare(
                  right.displayName,
                  'hu-HU',
                  {
                    sensitivity: 'base',
                  },
                ),
            ),
          );

          const availableIds = new Set(this.availableAssets().map(asset => asset.assetId));

          this.selectedAssetIds.update(ids => ids.filter(id => availableIds.has(id)));
        },
        error: (error: unknown) => {
          console.error('Failed to load package creation data.', error,);

          this.stores.set([]);
          this.assets.set([]);
          this.applicationUsers.set([]);
          this.loadFailed.set(true);

          this.showError(
            this.getErrorMessage(error, 'A csomaglétrehozáshoz szükséges adatok betöltése nem sikerült.')
          );
        },
      });
  }

  private validate(): string | null {
    const sourceStore = this.selectedSourceStore();
    const destinationStore = this.selectedDestinationStore();

    if (!sourceStore) {
      return 'A forrás telephely kiválasztása kötelező.';
    }

    if (!destinationStore) {
      return 'A céltelephely kiválasztása kötelező.';
    }

    if (sourceStore.id === destinationStore.id) {
      return 'A forrás és a céltelephely nem lehet azonos.';
    }

    if (
      !this.isValidRoute(sourceStore, destinationStore)
    ) {
      return 'Csomag csak egy áruház és a központ között hozható létre.';
    }

    if (this.selectedAssetIds().length === 0) {
      return 'Legalább egy eszközt ki kell választani.';
    }

    const availableIds = new Set(this.availableAssets().map(asset => asset.assetId));
    const containsUnavailableAsset = this.selectedAssetIds().some(assetId => !availableIds.has(assetId));

    if (containsUnavailableAsset) {
      return 'A kiválasztott eszközök között nem elérhető eszköz található. Ellenőrizd a kiválasztást.';
    }

    const courierId = this.courierUserId();

    if (courierId !== null && !this.applicationUsers().some(user => user.id === courierId)
    ) {
      return 'A kiválasztott futár nem érhető el. Válassz másik futárt.';
    }

    if (this.commentLength() > PackageCreate.commentMaxLength) {
      return `A megjegyzés legfeljebb ${PackageCreate.commentMaxLength} karakter lehet.`;
    }

    return null;
  }

  private isValidRoute(sourceStore: GetStoresResponse, destinationStore: GetStoresResponse): boolean {
    return (sourceStore.id !== destinationStore.id && this.isHeadquarters(sourceStore) !== this.isHeadquarters(destinationStore));
  }

  private isHeadquarters(store: GetStoresResponse): boolean {
    return (store.storeNumber === PackageCreate.headquartersStoreNumber);
  }

  private isAssetAvailableForSelectedRoute(asset: GetAssetsResponse, sourceStore: GetStoresResponse, destinationStore: GetStoresResponse): boolean {
    if (asset.assetStatus === 'Disposed' || asset.assignedUserId !== null) {
      return false;
    }

    if (this.isHeadquarters(sourceStore)) {
      return (asset.assetStatus === 'In_HQ' && asset.assignedStoreId === destinationStore.id);
    }

    return (asset.assetStatus === 'In_Store' && asset.assignedStoreId === sourceStore.id);
  }

  private normalizeOptionalText(value: string): string | null {
    const normalized = value.trim();

    return normalized.length > 0 ? normalized : null;
  }

  private showError(message: string): void {
    this.snackBar.open(
      message,
      'Bezárás',
      {
        duration: 6000,
        horizontalPosition: 'right',
        verticalPosition: 'bottom',
        politeness: 'assertive',
      },
    );
  }

  private getErrorMessage(error: unknown, fallbackMessage: string): string {
    if (!(error instanceof HttpErrorResponse)) {
      return fallbackMessage;
    }

    switch (error.status) {
      case 0:
        return 'Nem sikerült kapcsolódni a kiszolgálóhoz. Ellenőrizd a hálózati kapcsolatot, majd próbáld újra.';

      case 400:
      case 422:
        return 'A megadott adatok nem fogadhatók el. Ellenőrizd az útvonalat, a kiválasztott eszközöket és a többi adatot.';

      case 401:
        return 'A munkameneted lejárt, vagy nem vagy bejelentkezve. Jelentkezz be újra.';

      case 403:
        return 'Nincs jogosultságod a művelet végrehajtásához.';

      case 404:
        return 'A kért erőforrás nem található. Frissítsd az oldalt, majd próbáld újra.';

      case 408:
      case 504:
        return 'A kiszolgáló nem válaszolt időben. Próbáld újra később.';

      case 409:
        return 'A művelet ütközik az adatok jelenlegi állapotával. Frissítsd az adatokat, majd ellenőrizd újra a kiválasztást.';

      case 429:
        return 'Túl sok kérés érkezett rövid idő alatt. Várj egy kicsit, majd próbáld újra.';

      default:
        if (error.status >= 500) {
          return 'Kiszolgálóhiba történt. Próbáld újra később.';
        }

        return fallbackMessage;
    }
  }
}