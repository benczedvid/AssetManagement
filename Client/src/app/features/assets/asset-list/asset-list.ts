import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, ElementRef, inject, OnInit, signal, ViewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/notifications/notification-service';
import { CurrentUserApi } from '../../auth/current-user-api';
import { AssetApi } from '../data-access/asset-api';
import { GetAssetsResponse } from '../data-access/models/get-assets-response';
import { ASSET_STATUS_LABELS, AssetStatus } from '../data-access/models/asset-status';
import { ASSET_TYPE_LABELS, AssetType } from '../data-access/models/asset-type';

type AssetTableColumn =
  | 'assetName'
  | 'assetStatus'
  | 'assetType'
  | 'manufacturer'
  | 'model'
  | 'serialNumber'
  | 'macAddress'
  | 'wifiMacAddress'
  | 'imei'
  | 'assignedEmployeeName'
  | 'createdAtUtc'
  | 'assignedVendorName'
  | 'rfidTagId';

interface AssetTableColumnDefinition {
  readonly id: AssetTableColumn;
  readonly label: string;
  readonly required: boolean;
  readonly defaultVisible: boolean;
}

const ASSET_TABLE_COLUMNS_STORAGE_KEY = 'asset-management.asset-list.visible-columns';

@Component({
  selector: 'app-asset-list',
  standalone: true,
  imports: [
    RouterLink,
    MatPaginatorModule,
    MatSortModule,
    MatTableModule,
    MatMenuModule,
    MatCheckboxModule,
    DatePipe,
  ],
  templateUrl: './asset-list.html',
  styleUrl: './asset-list.css',
})
export class AssetList implements OnInit {
  private readonly assetApi = inject(AssetApi);
  private readonly currentUserApi = inject(CurrentUserApi);
  private readonly router = inject(Router);
  private readonly notificationService = inject(NotificationService);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly canManageAssets = computed(() => this.currentUserApi.canManageAssets());
  readonly statusLabels = ASSET_STATUS_LABELS;
  readonly typeLabels = ASSET_TYPE_LABELS;
  readonly searchTerm = signal('');
  readonly dataSource = new MatTableDataSource<GetAssetsResponse>([]);
  readonly hasActiveFilter = computed(() => this.searchTerm().trim().length > 0);
  readonly availableColumns: readonly AssetTableColumnDefinition[] = [
      {
        id: 'assetName',
        label: 'Név',
        required: true,
        defaultVisible: true,
      },
      {
        id: 'assetStatus',
        label: 'Állapot',
        required: true,
        defaultVisible: true,
      },
      {
        id: 'assetType',
        label: 'Típus',
        required: false,
        defaultVisible: true,
      },
      {
        id: 'manufacturer',
        label: 'Gyártó',
        required: false,
        defaultVisible: true,
      },
      {
        id: 'model',
        label: 'Modell',
        required: false,
        defaultVisible: true,
      },
      {
        id: 'serialNumber',
        label: 'Sorozatszám',
        required: false,
        defaultVisible: true,
      },
      {
        id: 'macAddress',
        label: 'MAC-cím',
        required: false,
        defaultVisible: false,
      },
      {
        id: 'wifiMacAddress',
        label: 'Wi-Fi MAC-cím',
        required: false,
        defaultVisible: false,
      },
      {
        id: 'imei',
        label: 'IMEI',
        required: false,
        defaultVisible: false,
      },
      {
        id: 'assignedEmployeeName',
        label: 'Jelenleg használja',
        required: false,
        defaultVisible: true,
      },
      {
        id: 'createdAtUtc',
        label: 'Létrehozás dátuma',
        required: false,
        defaultVisible: false,
      },
      {
        id: 'assignedVendorName',
        label: 'Beszállító',
        required: false,
        defaultVisible: false,
      },
      {
        id: 'rfidTagId',
        label: 'RFID címke azonosítója',
        required: false,
        defaultVisible: true,
      },
    ];

  readonly visibleColumns = signal<readonly AssetTableColumn[]>(this.getInitialVisibleColumns());
  readonly displayedColumns = computed(() => this.visibleColumns());
  readonly isDefaultColumnSelection = computed(() => {
      const defaultColumns =
        this.availableColumns
          .filter(
            column =>
              column.defaultVisible ||
              column.required
          )
          .map(column => column.id);

      const visibleColumns = this.visibleColumns();

      return (
        defaultColumns.length ===
        visibleColumns.length &&
        defaultColumns.every(
          (
            column,
            index
          ) =>
            visibleColumns[index] === column
        )
      );
    }
  );

  private paginatorInstance: MatPaginator | null = null;

  @ViewChild(MatSort)
  set sort(
    sort: MatSort | undefined
  ) {
    if (!sort) {
      return;
    }

    this.dataSource.sort = sort;
  }

  @ViewChild(MatPaginator)
  set paginator(
    paginator: MatPaginator | undefined
  ) {
    if (!paginator) {
      return;
    }

    this.paginatorInstance = paginator;
    this.dataSource.paginator = paginator;
  }

  @ViewChild('searchInput')
  private searchInput: ElementRef<HTMLInputElement> | undefined;

  constructor() {
    this.configureSorting();
    this.configureFiltering();
  }

  ngOnInit(): void {
    this.loadAssets();
  }

  loadAssets(): void {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.assetApi
      .getAll()
      .pipe(
        finalize(
          () =>
            this.isLoading.set(false)
        )
      )
      .subscribe({
        next: assets => {
          this.dataSource.data = [...assets];
        },

        error: (
          error: HttpErrorResponse
        ) => {
          console.error( 'Failed to load assets.', error);
          const message = this.getErrorMessage(error);
          this.errorMessage.set(message);
          this.notificationService.error(message);
        },
      });
  }

  openAssetDetails(assetId: string): void {
    void this.router.navigate(['/assets', assetId,]);
  }

  getAssetTypeLabel(assetType: AssetType | null | undefined): string {
    if (!assetType) {
      return 'Nincs megadva';
    }
    return this.typeLabels[assetType];
  }

  getAssetStatusLabel(assetStatus: AssetStatus | null | undefined): string {
        if (!assetStatus) {
      return 'Nincs megadva';
    }

    return this.statusLabels[assetStatus];
  }

  applyFilter(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value;

    this.searchTerm.set(value);
    this.dataSource.filter = value.trim().toLocaleLowerCase('hu');
    this.paginatorInstance?.firstPage();
  }

  clearFilter(): void {
    this.searchTerm.set('');
    this.dataSource.filter = '';

    this.paginatorInstance?.firstPage();

    queueMicrotask(() => {
      this.searchInput?.nativeElement.focus();
    });
  }

  get filteredAssetCount(): number {
    return this.dataSource.filteredData.length;
  }

  isColumnVisible(columnId: AssetTableColumn): boolean {
    return this.visibleColumns().includes(columnId);
  }

  toggleColumn(column: AssetTableColumnDefinition): void {
    if (column.required) {
      return;
    }

    const currentlyVisible = this.visibleColumns();

    const nextVisibleColumns =
      currentlyVisible.includes(column.id)
        ? currentlyVisible.filter(
          visibleColumn =>
            visibleColumn !== column.id
        )
        : [
          ...currentlyVisible,
          column.id,
        ];

    const orderedVisibleColumns =
      this.availableColumns
        .filter(
          availableColumn =>
            nextVisibleColumns.includes(
              availableColumn.id
            )
        )
        .map(availableColumn => availableColumn.id
        );

    this.visibleColumns.set(orderedVisibleColumns);
    this.saveVisibleColumns(orderedVisibleColumns);
  }

  resetVisibleColumns(): void {
    const defaultColumns = this.availableColumns
        .filter(
          column =>
            column.defaultVisible ||
            column.required
        )
        .map(column => column.id);

    this.visibleColumns.set(defaultColumns);
    this.saveVisibleColumns(defaultColumns);
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    const apiDetail = error.error?.detail;

    if (typeof apiDetail === 'string' && apiDetail.trim() !== '') {
      return apiDetail;
    }

    switch (error.status) {
      case 401:
        return ('Authentication is required to load assets.');
      case 403:
        return ('You do not have permission to view assets.');
      case 0:
         return ('Unable to connect to the server. Please check your network connection.');
      default:
        return ('An unexpected error occurred while loading assets.');
    }
  }

  private configureSorting(): void {
    this.dataSource.sortingDataAccessor = (
      asset: GetAssetsResponse,
      columnName: string
    ): string => {
      switch (
      columnName as AssetTableColumn
      ) {
        case 'assetName':
          return this.normalizeSortValue(
            asset.assetName
          );

        case 'assetStatus':
          return this.normalizeSortValue(
            this.getAssetStatusLabel(
              asset.assetStatus
            )
          );

        case 'assetType':
          return this.normalizeSortValue(
            this.getAssetTypeLabel(
              asset.assetType
            )
          );

        case 'manufacturer':
          return this.normalizeSortValue(
            asset.manufacturer
          );

        case 'model':
          return this.normalizeSortValue(
            asset.model
          );

        case 'serialNumber':
          return this.normalizeSortValue(
            asset.serialNumber
          );

        case 'macAddress':
          return this.normalizeSortValue(
            asset.macAddress
          );

        case 'wifiMacAddress':
          return this.normalizeSortValue(
            asset.wifiMacAddress
          );

        case 'imei':
          return this.normalizeSortValue(
            asset.imei
          );

        case 'assignedEmployeeName':
          return this.normalizeSortValue(
            asset.assignedEmployeeName
          );

        case 'createdAtUtc':
          return this.normalizeSortValue(
            asset.createdAtUtc
          );

        case 'assignedVendorName':
          return this.normalizeSortValue(
            asset.assignedVendorName
          );

        case 'rfidTagId':
          return this.normalizeSortValue(
            asset.rfidTagId
          );

        default:
          return '';
      }
    };

    this.dataSource.sortData = (
      data: GetAssetsResponse[],
      sort: MatSort
    ): GetAssetsResponse[] => {
      if (
        !sort.active ||
        (
          sort.direction !== 'asc' &&
          sort.direction !== 'desc'
        )
      ) {
        return data;
      }

      const direction:
        'asc' | 'desc' =
        sort.direction;

      return [
        ...data,
      ].sort(
        (
          firstAsset,
          secondAsset
        ) => {
          const firstValue =
            this.dataSource
              .sortingDataAccessor(
                firstAsset,
                sort.active
              );

          const secondValue =
            this.dataSource
              .sortingDataAccessor(
                secondAsset,
                sort.active
              );

          return this.compareSortValues(
            firstValue,
            secondValue,
            direction
          );
        }
      );
    };
  }

  private normalizeSortValue(
    value:
      | string
      | number
      | null
      | undefined
  ): string {
    return String(value ?? '').trim();
  }

  private compareSortValues(
    firstValue:
      string | number,
    secondValue:
      string | number,
    direction:
      'asc' | 'desc'
  ): number {
    const normalizedFirstValue =
      this.normalizeSortValue(
        firstValue
      );

    const normalizedSecondValue =
      this.normalizeSortValue(
        secondValue
      );

    const firstIsEmpty =
      normalizedFirstValue.length === 0;

    const secondIsEmpty =
      normalizedSecondValue.length === 0;

    if (
      firstIsEmpty &&
      secondIsEmpty
    ) {
      return 0;
    }

    /*
     * Empty values remain at the end
     * for both ascending and descending sorting.
     */
    if (firstIsEmpty) {
      return 1;
    }

    if (secondIsEmpty) {
      return -1;
    }

    const comparison =
      normalizedFirstValue.localeCompare(
        normalizedSecondValue,
        'hu',
        {
          numeric: true,
          sensitivity: 'base',
        }
      );

    return direction === 'asc'
      ? comparison
      : comparison * -1;
  }

  private configureFiltering(): void {
    this.dataSource.filterPredicate = (
      asset: GetAssetsResponse,
      filter: string
    ): boolean => {
      const normalizedFilter =
        this.normalizeFilterValue(
          filter
        );

      if (!normalizedFilter) {
        return true;
      }

      const searchableValues = [
        asset.assetName,
        this.getAssetStatusLabel(
          asset.assetStatus
        ),
        this.getAssetTypeLabel(
          asset.assetType
        ),
        asset.manufacturer,
        asset.model,
        asset.serialNumber,
        asset.macAddress,
        asset.wifiMacAddress,
        asset.imei,
        asset.assignedEmployeeName,
        asset.createdAtUtc,
        asset.assignedVendorName,
        asset.rfidTagId,
      ];

      return searchableValues.some(
        value =>
          this.normalizeFilterValue(
            value
          ).includes(
            normalizedFilter
          )
      );
    };
  }

  private normalizeFilterValue(
    value:
      | string
      | number
      | null
      | undefined
  ): string {
    return String(value ?? '')
      .trim()
      .toLocaleLowerCase('hu')
      .normalize('NFD')
      .replace(
        /[\u0300-\u036f]/g,
        ''
      );
  }

  private getInitialVisibleColumns():
    readonly AssetTableColumn[] {
    const defaultColumns =
      this.availableColumns
        .filter(
          column =>
            column.defaultVisible ||
            column.required
        )
        .map(
          column => column.id
        );

    const storedValue =
      localStorage.getItem(
        ASSET_TABLE_COLUMNS_STORAGE_KEY
      );

    if (!storedValue) {
      return defaultColumns;
    }

    try {
      const parsedValue: unknown =
        JSON.parse(storedValue);

      if (!Array.isArray(parsedValue)) {
        return defaultColumns;
      }

      const validStoredColumns =
        parsedValue.filter(
          (
            value
          ): value is AssetTableColumn =>
            typeof value === 'string' &&
            this.isAssetTableColumn(value)
        );

      const requiredColumns =
        this.availableColumns
          .filter(
            column => column.required
          )
          .map(
            column => column.id
          );

      const selectedColumns =
        new Set<AssetTableColumn>([
          ...validStoredColumns,
          ...requiredColumns,
        ]);

      return this.availableColumns
        .filter(
          column =>
            selectedColumns.has(
              column.id
            )
        )
        .map(
          column => column.id
        );
    } catch (error) {
      console.error(
        'The saved asset table column configuration is invalid.',
        error
      );

      localStorage.removeItem(
        ASSET_TABLE_COLUMNS_STORAGE_KEY
      );

      return defaultColumns;
    }
  }

  private isAssetTableColumn(
    value: string
  ): value is AssetTableColumn {
    return this.availableColumns.some(
      column =>
        column.id === value
    );
  }

  private saveVisibleColumns(
    columns:
      readonly AssetTableColumn[]
  ): void {
    localStorage.setItem(
      ASSET_TABLE_COLUMNS_STORAGE_KEY,
      JSON.stringify(columns)
    );
  }
}