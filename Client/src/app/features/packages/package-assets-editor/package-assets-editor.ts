import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  TemplateRef,
  ViewChild,
  computed,
  inject,
  input,
  output,
  signal } from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  concatMap,
  defer,
  finalize,
  from,
  map,
  of,
  tap,
  toArray } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { AssetApi } from '../../assets/data-access/asset-api';
import type { GetAssetsResponse } from '../../assets/models/get-assets-response';
import { PackagesApi } from '../data-access/packages-api';
import type { PackageDetails } from '../data-access/packages-api';
import { MatIconModule } from '@angular/material/icon';

interface ConfirmationDialogData {
  readonly title: string;
  readonly message: string;
  readonly confirmLabel: string;
}

interface ConfirmationDialogContext {
  readonly $implicit: ConfirmationDialogData;
}

interface AddAssetResult {
  readonly assetId: string;
  readonly success: boolean;
  readonly message: string;
}

@Component({
  selector: 'app-package-assets-editor',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatDialogModule,
    MatDividerModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTableModule,
    MatIconModule,
  ],
  templateUrl: './package-assets-editor.html',
  styleUrl: './package-assets-editor.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageAssetsEditor implements OnInit {
  private readonly assetApi = inject(AssetApi);
  private readonly packagesApi = inject(PackagesApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  @ViewChild('confirmationDialog', { static: true })
  private confirmationDialogTemplate!: TemplateRef<ConfirmationDialogContext>;
  private activeDialogRef: MatDialogRef<unknown, boolean> | null = null;

  private readonly saving = signal(false);
  private readonly confirming = signal(false);

  readonly packageDetails = input.required<PackageDetails>();
  readonly assetsChanged = output<void>();
  readonly editorVisible = signal(true);
  readonly displayedColumns: string[] = [
    'select',
    'assetName',
    'serialNumber',
    'store',
    'status',
  ];

  /*
   * Asset loading.
   */

  readonly assets = signal<readonly GetAssetsResponse[]>([]);
  readonly assetsLoading = signal(false);
  readonly assetsLoaded = signal(false);
  readonly assetsLoadFailed = signal(false);

  /*
   * Search and selection.
   */

  readonly searchText = signal('');
  readonly selectedAssetIds = signal<readonly string[]>([]);
  readonly selectedAssets = computed(() => {
    const selectedIds = new Set(this.selectedAssetIds());

    return this.assets().filter(asset => selectedIds.has(asset.assetId));
  });

  /*
   * Operation state.
   */

  readonly operationInProgress = computed(() => this.saving() || this.confirming());
  readonly requestInProgress = computed(() => this.saving());
  readonly assetPendingRemovalId = signal<string | null>(null);

  /*
   * Package state.
   */

  readonly isCreated = computed(() => this.packageDetails().status === 'Created');

  readonly packageAssetIds = computed(() => {
    return new Set(this.packageDetails().items.map(item => item.assetId));
  });

  /*
   * Filtered table data.
   */

  readonly filteredAssets = computed(() => {
    const search = this.normalizeSearchText(this.searchText());
    const packageAssetIds = this.packageAssetIds();

    return this.assets()
      .filter(asset => !packageAssetIds.has(asset.assetId))
      .filter(asset => {
        if (search.length === 0) {
          return true;
        }

        const values = [
          asset.assetName,
          asset.serialNumber,
          asset.assignedStoreNumber,
        ];

        return values.some(value => this.normalizeSearchText(value ?? '').includes(search));
      })
      .sort((left, right) =>
        left.assetName.localeCompare(
          right.assetName,
          'hu-HU',
          {
            sensitivity: 'base',
            numeric: true,
          },
        ),
      );
  });

  readonly allFilteredAssetsSelected = computed(() => {
    const filtered = this.filteredAssets();
    const selectedIds = new Set(this.selectedAssetIds());

    return (filtered.length > 0 && filtered.every(asset => selectedIds.has(asset.assetId)));
  });

  readonly someFilteredAssetsSelected = computed(() => {
    const selectedIds = new Set(this.selectedAssetIds());

    return (this.filteredAssets().some(asset => selectedIds.has(asset.assetId)) && !this.allFilteredAssetsSelected());
  });

  readonly canAddAsset = computed(() => {
    const selected = this.selectedAssets();

    return (
      this.isCreated() &&
      this.assetsLoaded() &&
      !this.assetsLoading() &&
      !this.assetsLoadFailed() &&
      !this.operationInProgress() &&
      selected.length > 0 &&
      selected.length ===
      this.selectedAssetIds().length &&
      selected.every(asset => !this.packageAssetIds().has(asset.assetId))
    );
  });

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.activeDialogRef?.close(false);
    });
  }

  ngOnInit(): void {
    this.loadAssets();
  }

  /*
   * Editor visibility.
   */

  openEditor(): void {
    if (
      !this.isCreated() ||
      this.operationInProgress()
    ) {
      return;
    }

    this.editorVisible.set(true);

    if (
      !this.assetsLoaded() &&
      !this.assetsLoading()
    ) {
      this.loadAssets();
    }
  }

  closeEditor(): void {
    if (this.operationInProgress()) {
      return;
    }

    if (this.selectedAssetIds().length === 0 && this.searchText().trim().length === 0) {
      this.hideEditor();
      return;
    }

    this.openConfirmation(
      {
        title: 'Kiválasztás elvetése',
        message:
          'Biztosan bezárod a szerkesztőt? A kijelölt eszközök nem kerülnek hozzáadásra.',
        confirmLabel: 'Elvetés és bezárás',
      },
      () => this.hideEditor(),
    );
  }

  /*
   * Load assets.
   */

  loadAssets(): void {
    if (this.assetsLoading() || this.operationInProgress()) {
      return;
    }

    this.assetsLoading.set(true);
    this.assetsLoadFailed.set(false);

    this.assetApi.getAll()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.assetsLoading.set(false);
        }),
      )
      .subscribe({
        next: assets => {
          this.assets.set(assets);
          this.assetsLoaded.set(true);

          const packageAssetIds = this.packageAssetIds();
          const selectableIds = new Set(
            assets
              .filter(asset => !packageAssetIds.has(asset.assetId))
              .map(asset => asset.assetId),
          );

          this.selectedAssetIds.update(ids => ids.filter(id => selectableIds.has(id)));
        },
        error: (error: unknown) => {
          console.error('Failed to load assets.', error);

          this.assets.set([]);
          this.assetsLoaded.set(false);
          this.assetsLoadFailed.set(true);

          this.showError(
            this.getErrorMessage(error, 'Az eszközök betöltése nem sikerült.'),
          );
        },
      });
  }

  /*
   * Search and multi-selection.
   */

  updateSearchText(value: string): void {
    if (this.operationInProgress() || !this.isCreated()) {
      return;
    }

    this.searchText.set(value ?? '');
  }

  isAssetSelected(assetId: string): boolean {
    return this.selectedAssetIds().includes(assetId);
  }

  toggleAsset(assetId: string, checked: boolean): void {
    if (
      this.operationInProgress() ||
      !this.isCreated() ||
      this.assetsLoading() ||
      this.assetsLoadFailed() ||
      this.packageAssetIds().has(assetId)
    ) {
      return;
    }

    const exists = this.assets().some(asset => asset.assetId === assetId);

    if (!exists) {
      return;
    }

    const selectedIds = new Set(this.selectedAssetIds());

    if (checked) {
      selectedIds.add(assetId);
    } else {
      selectedIds.delete(assetId);
    }

    this.selectedAssetIds.set([...selectedIds]);
  }

  toggleAllFilteredAssets(checked: boolean): void {
    if (
      this.operationInProgress() ||
      !this.isCreated() ||
      this.assetsLoading() ||
      this.assetsLoadFailed()
    ) {
      return;
    }

    const selectedIds = new Set(this.selectedAssetIds());

    for (const asset of this.filteredAssets()) {
      if (checked) {
        selectedIds.add(asset.assetId);
      } else {
        selectedIds.delete(asset.assetId);
      }
    }

    this.selectedAssetIds.set([...selectedIds]);
  }

  clearSelection(): void {
    if (this.operationInProgress()) {
      return;
    }

    this.selectedAssetIds.set([]);
  }

  /*
   * Add selected assets.
   */

  addAsset(): void {
    if (!this.canAddAsset()) {
      return;
    }

    const packageId = this.packageDetails().id;
    const assets = [...this.selectedAssets()];

    const preview = assets
      .slice(0, 8)
      .map(asset =>
        `• ${asset.assetName}: ${
          asset.serialNumber || 'Nincs gyári szám'
        }`,
      );

    if (assets.length > 8) {
      preview.push(
        `További ${assets.length - 8} eszköz.`,
      );
    }

    this.openConfirmation(
      {
        title: 'Eszközök hozzáadása',
        message: [
          `Hozzáadod a kijelölt ${assets.length} eszközt a csomaghoz?`,
          '',
          ...preview,
        ].join('\n'),
        confirmLabel: `Hozzáadás (${assets.length})`,
      },
      () => this.executeAddAssets(
        packageId,
        assets.map(asset => asset.assetId),
      ),
    );
  }

  private executeAddAssets(packageId: string, assetIds: readonly string[]): void {
    const availableIds = new Set(this.assets().map(asset => asset.assetId));

    if (
      !this.isCreated() ||
      this.operationInProgress() ||
      this.assetsLoading() ||
      this.assetsLoadFailed() ||
      this.packageDetails().id !== packageId ||
      assetIds.length === 0 ||
      assetIds.some(assetId => !availableIds.has(assetId) || this.packageAssetIds().has(assetId))
    ) {
      this.showError('A csomag vagy a kijelölt eszközök adatai megváltoztak. Ellenőrizd újra a kiválasztást.');

      return;
    }

    this.saving.set(true);

    from(assetIds)
      .pipe(
        concatMap(assetId =>
          defer(() => {
            if (
              this.packageDetails().id !== packageId ||
              !this.isCreated()
            ) {
              return of<AddAssetResult>({
                assetId,
                success: false,
                message:
                  'A csomag adatai megváltoztak, ezért az eszköz hozzáadása kimaradt.',
              });
            }

            return this.packagesApi
              .addAsset(packageId, { assetId })
              .pipe(
                tap(() => {
                  this.selectedAssetIds.update(ids =>
                    ids.filter(id => id !== assetId),
                  );
                }),
                map((): AddAssetResult => ({
                  assetId,
                  success: true,
                  message: '',
                })),
              );
          }).pipe(
            catchError((error: unknown) => {
              console.error('Failed to add package asset.', assetId, error);

              return of<AddAssetResult>({
                assetId,
                success: false,
                message: this.getErrorMessage(error, 'Az eszköz hozzáadása nem sikerült.'),
              });
            }),
          ),
        ),
        toArray(),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.saving.set(false);
        }),
      )
      .subscribe(results => {
        const succeeded = results.filter(
          result => result.success,
        );

        const failed = results.filter(
          result => !result.success,
        );

        if (failed.length === 0) {
          this.hideEditor();

          this.showSuccess(`${succeeded.length} eszköz sikeresen hozzáadva a csomaghoz.`);
        } else {
          this.showError(
            [
              `${succeeded.length} eszköz hozzáadva.`,
              `${failed.length} eszköz hozzáadása nem sikerült.`,
              'A sikertelen eszközök kijelölve maradtak.',
              failed[0].message,
            ].join(' '),
          );
        }

        if (succeeded.length > 0) {
          this.assetsChanged.emit();
        }
      });
  }

  /*
   * Remove asset.
   */

  requestRemoveAsset(assetId: string): void {
    if (!this.isCreated() || this.operationInProgress()) {
      return;
    }

    const exists = this.packageDetails().items.some(item => item.assetId === assetId);

    if (!exists) {
      return;
    }

    const packageId = this.packageDetails().id;
    const asset = this.assets().find(item => item.assetId === assetId);

    this.assetPendingRemovalId.set(assetId);

    this.openConfirmation(
      {
        title: 'Eszköz eltávolítása',
        message: asset
          ? [
              'Eltávolítod az alábbi eszközt a csomagból?',
              '',
              `Eszköz: ${asset.assetName}`,
              `Gyári szám: ${
                asset.serialNumber || 'Nincs megadva'
              }`,
              '',
              'Az eszköz nem törlődik a nyilvántartásból.',
            ].join('\n')
          : [
              'Biztosan eltávolítod az eszközt a csomagból?',
              '',
              'Az eszköz nem törlődik a nyilvántartásból.',
            ].join('\n'),
        confirmLabel: 'Eltávolítás',
      },
      () => this.executeRemoveAsset(packageId, assetId),
      () => {
        this.assetPendingRemovalId.set(null);
      },
    );
  }

  cancelRemoveAsset(): void {
    if (this.saving()) {
      return;
    }

    if (this.assetPendingRemovalId() === null) {
      return;
    }

    this.activeDialogRef?.close(false);
    this.assetPendingRemovalId.set(null);
  }

  confirmRemoveAsset(): void {
    if (!this.confirming() || this.assetPendingRemovalId() === null || this.saving()) {
      return;
    }

    this.activeDialogRef?.close(true);
  }

  private executeRemoveAsset(packageId: string, assetId: string): void {
    const exists = this.packageDetails().items.some(item => item.assetId === assetId);

    if (!this.isCreated() || this.operationInProgress() || this.packageDetails().id !== packageId || !exists) {
      this.assetPendingRemovalId.set(null);

      this.showError('A csomag adatai megváltoztak. Frissítsd az adatokat, majd próbáld újra.');

      return;
    }

    this.saving.set(true);

    this.packagesApi.removeAsset(packageId, assetId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.saving.set(false);
          this.assetPendingRemovalId.set(null);
        }),
      )
      .subscribe({
        next: () => {
          this.assetPendingRemovalId.set(null);

          this.showSuccess('Az eszköz sikeresen eltávolítva a csomagból.');

          this.assetsChanged.emit();
        },
        error: (error: unknown) => {
          console.error('Failed to remove package asset.', error);

          this.showError(
            this.getErrorMessage(error,  'Az eszköz eltávolítása nem sikerült.'),
          );
        },
      });
  }

  /*
   * Confirmation dialog.
   */

  private openConfirmation(data: ConfirmationDialogData, onConfirmed: () => void, onCancelled: () => void = () => {}): void {
    if (this.operationInProgress()) {
      return;
    }

    this.confirming.set(true);

    const dialogRef = this.dialog.open<unknown, ConfirmationDialogData, boolean >(
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
        } else {
          onCancelled();
        }
      });
  }

  getAssetStatusLabel(status: string): string {
    switch (status) {
      case 'In_HQ':
        return 'Központban';

      case 'In_Store':
        return 'Áruházban';

      case 'Disposed':
        return 'Selejtezett';
      
      case 'Used_In_Store':
        return 'Áruházi használatban';
      
      case 'Used_In_HQ':
        return 'Központi használatban';

      case 'Route_To_HQ':
        return 'Úton a központba';

      case 'Route_To_Store':
        return 'Úton az áruházba';

      case 'Route_To_Service':
        return 'Úton a szervizbe';
      
      case 'In_Service':
        return 'Szervízben';

      default:
        return status;
    }
  }

  /*
   * Helpers.
   */

  private hideEditor(): void {
    this.editorVisible.set(false);
    this.resetSelection();
  }

  private resetSelection(): void {
    this.searchText.set('');
    this.selectedAssetIds.set([]);
  }

  private normalizeSearchText(value: string): string {
    return value
      .normalize('NFD')
      .replace(/\p{Diacritic}/gu, '')
      .trim()
      .toLocaleLowerCase('hu-HU');
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

  private showSuccess(message: string): void {
    this.snackBar.open(
      message,
      'Bezárás',
      {
        duration: 4000,
        horizontalPosition: 'right',
        verticalPosition: 'bottom',
        politeness: 'polite',
      },
    );
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (!(error instanceof HttpErrorResponse)) {
      return fallback;
    }

    switch (error.status) {
      case 0:
        return 'A kiszolgáló nem érhető el. Módosítás esetén a művelet eredménye nem ellenőrizhető. Frissítsd a csomag adatait, mielőtt újra próbálkozol.';

      case 400:
      case 422:
        return 'A megadott adatok nem fogadhatók el. Ellenőrizd a kiválasztott eszközt és a csomag útvonalát.';

      case 401:
        return 'A művelethez bejelentkezés szükséges. Jelentkezz be újra.';

      case 403:
        return 'Nincs jogosultságod a művelet végrehajtásához.';

      case 404:
        return 'A kért erőforrás nem található. Frissítsd a csomag adatait.';

      case 409:
        return 'A csomag vagy az eszköz jelenlegi állapota nem teszi lehetővé a műveletet. Frissítsd az adatokat, majd ellenőrizd újra a kiválasztást.';

      case 408:
      case 504:
        return 'A kiszolgáló nem válaszolt időben. Módosítás esetén ellenőrizd a csomag aktuális tartalmát, mielőtt újra próbálkozol.';

      case 429:
        return 'Túl sok kérés érkezett. Várj egy kicsit, majd próbáld újra.';

      default:
        return fallback;
    }
  }
}