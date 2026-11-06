import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, ElementRef, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { NotificationService } from '../../../core/notifications/notification-service';
import { CurrentUserApi } from '../../auth/current-user-api';
import { AssetApi } from '../data-access/asset-api';
import { GetAssetsResponse } from '../models/get-assets-response';
import { ASSET_STATUS_LABELS, AssetStatus } from '../models/asset-status';
import { ASSET_TYPE_LABELS, AssetType } from '../models/asset-type';
import { SideMenuComponent, SideMenuItem } from '../../../shared/side-menu/side-menu';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, finalize, Subject } from 'rxjs';

type AssetTableColumn =
  | 'assetName'
  | 'assetStatus'
  | 'assetType'
  | 'assignedStoreNumber'
  | 'manufacturer'
  | 'model'
  | 'serialNumber'
  | 'macAddress'
  | 'wifiMacAddress'
  | 'imei'
  | 'assignedEmployeeName'
  | 'createdAtUtc'
  | 'assignedVendorName'
  | 'rfidTagId'
  | 'warrantyEndDate';

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
    SideMenuComponent
  ],
  templateUrl: './asset-list.html',
  styleUrl: './asset-list.css',
})
export class AssetList implements OnInit {
  private readonly assetApi = inject(AssetApi);
  private readonly currentUserApi = inject(CurrentUserApi);
  private readonly router = inject(Router);
  private readonly notificationService = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  private readonly searchChanged = new Subject<string>();
  readonly storeMenuItems = signal<SideMenuItem[]>([]);
  readonly selectedStores = signal<string[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly canManageAssets = computed(() => this.currentUserApi.canManageAssets());
  readonly statusLabels = ASSET_STATUS_LABELS;
  readonly typeLabels = ASSET_TYPE_LABELS;
  readonly searchTerm = signal('');
  readonly dataSource = new MatTableDataSource<GetAssetsResponse>([]);
  readonly hasActiveSearch = computed(() => this.searchTerm().trim().length > 0);
  readonly hasActiveFilter = computed(() => {
    const defaultTypes = this.getDefaultSelectedTypes();
    const defaultStatuses = this.getDefaultSelectedStatuses();
    const selectedTypes = this.selectedTypes();
    const selectedStatuses = this.selectedStatuses();
    const typeFilterActive = selectedTypes.length !== defaultTypes.length || !defaultTypes.every(type => selectedTypes.includes(type));
    const statusFilterActive = selectedStatuses.length !== defaultStatuses.length || !defaultStatuses.every(status => selectedStatuses.includes(status));
    const storeFilterActive = this.selectedStores().length > 0;
    return (
      this.searchTerm().trim().length > 0 ||
      typeFilterActive ||
      statusFilterActive ||
      storeFilterActive
    );
  });
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
        id: 'assignedStoreNumber',
        label: 'Áruház',
        required: false,
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
      {
        id: 'warrantyEndDate',
        label: 'Garancia vége',
        required: false,
        defaultVisible: false
      },
    ];
  private readonly storeColumnIds = new Set<AssetTableColumn>([
    'assetName',
    'assetStatus',
    'assetType',
    'manufacturer',
    'model',
    'serialNumber',
    'macAddress',
    'wifiMacAddress',
    'imei',
    'assignedEmployeeName',
    'rfidTagId',
  ])
  readonly isStoreUser = computed(() => this.currentUserApi.hasAnyRole('StoreManagement', 'StoreAdministrator'))
  readonly canFilterByStore = computed(() => this.currentUserApi.hasAnyRole('ApplicationAdministrator', 'HotlineAgent', 'CentralUser'));
  readonly selectTableColumns = computed(() => {
    if(!this.currentUserApi.isInitialized()) {
      return [] as readonly AssetTableColumnDefinition[];
    }
    return this.isStoreUser() ? this.availableColumns.filter(column => this.storeColumnIds.has(column.id)) : this.availableColumns
  })
  readonly visibleColumns = signal<readonly AssetTableColumn[]>(this.getInitialVisibleColumns());
  readonly displayedColumns = computed(() => { 
    const allowedIds = new Set(this.selectTableColumns().map(column => column.id));
    return this.visibleColumns().filter(column => allowedIds.has(column));
   });
  readonly isDefaultColumnSelection = computed(() => {
    const defaultColumns = this.selectTableColumns()
      .filter(column => column.defaultVisible || column.required)
      .map(column => column.id);

    const displayedColumns = this.displayedColumns();

    return (
      defaultColumns.length === displayedColumns.length &&
      defaultColumns.every(
        (column, index) => displayedColumns[index] === column
      )
    );
  });
  private allAssets: GetAssetsResponse[] = [];
  readonly typeMenuItems: SideMenuItem[] = [
    { label: 'Vékonykliens', key: 'ThinClient' },
    { label: 'Notebook', key: 'Notebook' },
    { label: 'Asztali számítógép', key: 'DesktopComputer' },
    { label: 'Monitor', key: 'Monitor' },
    { label: 'PDT', key: 'PDT' },
    { label: 'Nyomtató', key: 'Printer' },
  ];
  readonly statusMenuItems: SideMenuItem[] = [
    { label: 'Központban', key: 'In_HQ' },
    { label: 'Központi használatban', key: 'Used_In_HQ' },
    { label: 'Áruházban', key: 'In_Store' },
    { label: 'Áruházi használatban', key: 'Used_In_Store' },
    { label: 'Szervizben', key: 'In_Service' },
    { label: 'Úton az áruházba', key: 'Route_To_Store' },
    { label: 'Úton a központba', key: 'Route_To_HQ' },
    { label: 'Úton a szervizbe', key: 'Route_To_Service' },
    { label: 'Selejtezve', key: 'Disposed' },
  ];
  readonly canViewDisposed = computed(() => this.currentUserApi.hasAnyRole('ApplicationAdministrator', 'HotlineAgent'));
  readonly visibleStatusMenuItems = computed(() => this.canViewDisposed() ? this.statusMenuItems : this.statusMenuItems.filter(item => item.key !== 'Disposed'));
  readonly selectedTypes = signal<string[]>([]);
  readonly selectedStatuses = signal<string[]>(
    this.statusMenuItems
    .map(item => item.key)
    .filter((key): key is string => key !== null && key !== 'Disposed')
    );
  private paginatorInstance: MatPaginator | null = null;
  readonly sidebarOpen = signal(true);
  toggleSidebar(): void {
    this.sidebarOpen.update(open => !open);
  }

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
    this.configureUrlFiltering();
    this.configureSearchUrlSync();
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
          this.allAssets = [...assets];
          if (this.canFilterByStore()) {
            const stores = [...new Map(assets.filter(asset => asset.assignedStoreId).map(asset => 
              [asset.assignedStoreId,{ key: asset.assignedStoreId, label: asset.assignedStoreNumber }])).values()].sort((a, b) =>
                  a.label.localeCompare(b.label, 'hu', { numeric: true }));
            this.storeMenuItems.set(stores);
          }
          this.applyMenuFilters();
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
    void this.router.navigate(['/assets', assetId],
      {
        queryParamsHandling: 'preserve'
      }
    );
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
  //Filtering and search methods
  applyFilter(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value;

    this.searchTerm.set(value);
    this.applySearchFilter(value);
    this.searchChanged.next(value);
  }

  clearFilter(): void {
    this.searchTerm.set('');
    this.applySearchFilter('');

    void this.updateQueryParams({ q: null });
    queueMicrotask(() => { this.searchInput?.nativeElement.focus(); });
  }

  private applySearchFilter(value: string): void {
    this.dataSource.filter = this.normalizeFilterValue(value);
    this.paginatorInstance?.firstPage();
  }

  get filteredAssetCount(): number {
    return this.dataSource.filteredData.length;
  }

  isColumnVisible(columnId: AssetTableColumn): boolean {
    return this.displayedColumns().includes(columnId);
  }

  toggleColumn(column: AssetTableColumnDefinition): void {
    if (column.required || !this.selectTableColumns().some(allowedColumn => allowedColumn.id === column.id)) {
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
    const allowedIds = new Set(
      this.selectTableColumns().map(column => column.id)
    );

    const preservedColumns = this.visibleColumns().filter(
      column => !allowedIds.has(column)
    );

    const defaultColumns = this.selectTableColumns()
      .filter(column => column.defaultVisible || column.required)
      .map(column => column.id);

    const selectedColumns = new Set<AssetTableColumn>([
      ...preservedColumns,
      ...defaultColumns,
    ]);

    const orderedColumns = this.availableColumns
      .filter(column => selectedColumns.has(column.id))
      .map(column => column.id);

    this.visibleColumns.set(orderedColumns);
    this.saveVisibleColumns(orderedColumns);
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
          case 'assignedStoreNumber':
            return this.normalizeSortValue(
              asset.assignedStoreNumber
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
        case 'warrantyEndDate':
          return this.normalizeSortValue(asset.warrantyEndDate);

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
    firstValue: string | number,
    secondValue: string | number,
    direction: 'asc' | 'desc'
  ): number {
    const normalizedFirstValue = this.normalizeSortValue(firstValue);

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

    return direction === 'asc' ? comparison : comparison * -1;
  }

  private configureFiltering(): void {
    this.dataSource.filterPredicate = (asset: GetAssetsResponse, filter: string): boolean => {
      const normalizedFilter = this.normalizeFilterValue(filter);

      if (!normalizedFilter) {
        return true;
      }

      const searchableValues = [
        asset.assetName,
        this.getAssetStatusLabel(asset.assetStatus),
        this.getAssetTypeLabel(asset.assetType),
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
        asset.warrantyEndDate,
        asset.assignedStoreNumber,
      ];

      return searchableValues.some(
        value => this.normalizeFilterValue(value).includes(normalizedFilter)
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
    const defaultColumns = this.availableColumns
        .filter(column => column.defaultVisible || column.required)
        .map(column => column.id);

    const storedValue = localStorage.getItem(ASSET_TABLE_COLUMNS_STORAGE_KEY);

    if (!storedValue) {
      return defaultColumns;
    }

    try {
      const parsedValue: unknown = JSON.parse(storedValue);

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

  //Menu filter methods
  onTypeSelected(event: { key: string | null; checked: boolean;}): void {
    if (event.key === null) {
      return;
    }

    this.selectedTypes.update(current => event.checked
        ? [...new Set([...current, event.key!])] : current.filter(key => key !== event.key)
    );
    this.updateMenuQueryParams();
  }

  onStatusSelected(event: { key: string | null; checked: boolean;}): void {
    if (event.key === null) {
      return;
    }

    if (event.key === 'Disposed' && !this.canViewDisposed() ) {
      return;
    }

    this.selectedStatuses.update(current => event.checked
        ? [...new Set([...current, event.key!])] : current.filter(key => key !== event.key));

    this.updateMenuQueryParams();
  }

  onStoreSelected(event: { key: string | null; checked: boolean }): void {
    const storeId = event.key;
      if (storeId === null) {
            return;
            }
      this.selectedStores.update(current => event.checked ? [...new Set([...current, storeId])] : current.filter(id => id !== storeId));

  this.updateMenuQueryParams();
  }
  
  private updateMenuQueryParams(): void {
    const allTypes = this.typeMenuItems.map(item => item.key).filter((key): key is string => key !== null);
    const allStatuses = this.visibleStatusMenuItems().map(item => item.key).filter((key): key is string => key !== null);
    const selectedTypeSet = new Set(this.selectedTypes());
    const selectedStatusSet = new Set(this.selectedStatuses());
    const selectedTypes = allTypes.filter(type => selectedTypeSet.has(type));
    const selectedStatuses = allStatuses.filter(status => selectedStatusSet.has(status));
    const allTypesSelected = selectedTypes.length === allTypes.length;
    const allStatusesSelected = selectedStatuses.length === allStatuses.length;
    const allStores = this.storeMenuItems().map(item => item.key).filter((key): key is string => key !== null);
    const selectedStoreSet = new Set(this.selectedStores());
    const selectedStores = allStores.filter(store => selectedStoreSet.has(store));

    void this.updateQueryParams({
      types: selectedTypes.length === 0 ? 'none' : selectedTypes,
      statuses: allStatusesSelected ? null : selectedStatuses.length === 0 ? 'none' : selectedStatuses,
      stores: selectedStores.length === 0 ? null : selectedStores
    });
  }

  private applyMenuFilters(): void {
    const types = new Set(this.selectedTypes());
    const statuses = new Set(this.selectedStatuses());
    const canViewDisposed = this.canViewDisposed();
    const stores = new Set(this.selectedStores());

    this.dataSource.data = this.allAssets.filter(asset => {
      const assignedStoreId = asset.assignedStoreId.trim().toLowerCase();

      return (
        asset.assetType != null &&
        types.has(asset.assetType) &&
        asset.assetStatus != null &&
        statuses.has(asset.assetStatus) &&
        (asset.assetStatus !== 'Disposed' || canViewDisposed) &&
        (stores.size === 0 || stores.has(asset.assignedStoreId))
      );
    });

    this.applySearchFilter(this.searchTerm());
  }

  //URL filtering and synchronization
  private configureUrlFiltering(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(params => {
        const search = params.get('q')?.trim() ?? '';
        const typeParams = params.getAll('types');
        const statusParams = params.getAll('statuses');
        const storeParams = params.getAll('stores');

        this.searchTerm.set(search);
        this.selectedTypes.set(this.parseSelectedTypes(typeParams));
        this.selectedStatuses.set(this.parseSelectedStatuses(statusParams));
        this.selectedStores.set(this.parseSelectedStores(storeParams));
        this.applyMenuFilters();
      });
  }
  private parseSelectedTypes(values: readonly string[]): string[] {
    if (values.length === 0) {
      return this.getDefaultSelectedTypes();
    }
    if (values.includes('none')) {
      return [];
    }
    const validTypes = new Set(this.typeMenuItems
        .map(item => item.key)
        .filter((key): key is string => key !== null));

    return [...new Set(values.filter(value => validTypes.has(value)))];
  }
  private parseSelectedStatuses(values: readonly string[]): string[] {
    if (values.length === 0) {
      return this.getDefaultSelectedStatuses();
    }
    if (values.includes('none')) {
      return [];
    }
    const validStatuses = new Set(this.getDefaultSelectedStatuses()
    );
    return [...new Set(values.filter(value => validStatuses.has(value)))];
  }
  private parseSelectedStores(values: readonly string[]): string[] {
    return [...new Set(values)];
  }

  private getDefaultSelectedTypes(): string[] {
  return ['PDT'];
  }

  private getDefaultSelectedStatuses(): string[] {
    return this.visibleStatusMenuItems().map(item => item.key).filter((key): key is string => key !== null);
  }

  private configureSearchUrlSync(): void {
  this.searchChanged.pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
    .subscribe(search => {
      void this.updateQueryParams({ q: search.trim() || null });
    });
  }
  private updateQueryParams(queryParams: Params): Promise<boolean> {
    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }
}