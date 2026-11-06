import { DatePipe } from '@angular/common';
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
  signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { A11yModule } from '@angular/cdk/a11y';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Observable, Subscription, finalize } from 'rxjs';
import { AssetApi } from '../../assets/data-access/asset-api';
import type { GetAssetsResponse } from '../../assets/models/get-assets-response';
import { VendorApiService } from '../../vendors/data-access/vendor-api';
import type { GetVendorsResponse } from '../../vendors/data-access/models/get-vendors-response';
import { ServiceCasesApi } from '../data-access/service-cases-api';
import type { OpenServiceCaseRequest } from '../models/open-service-case-request';
import type { RequireExternalServiceRequest } from '../models/require-external-service-request';
import type { ServiceCaseAction } from '../models/service-case-action';
import type { ServiceCaseCommentRequest } from '../models/service-case-comment-request';
import type { ServiceCaseDetails } from '../models/service-case-details';
import type { ServiceCaseListItem } from '../models/service-case-list-item';
import type { DisposeServiceCaseAssetRequest } from '../models/dispose-service-case-asset-request';
import { ServiceIssueType, getServiceIssueTypeLabel } from '../models/service-issue-type';
import { ServiceCaseStatus, getServiceCaseStatusLabel } from '../models/service-case-status';
import type { ServiceCaseStatusValue } from '../models/service-case-status';
import { getServiceCaseActionLabel } from '../models/service-case-action';
import { getServiceCaseEventTypeLabel } from '../models/service-case-event-type';

interface ConfirmationData {
  title: string;
  message: string;
  detail: string | null;
  confirmLabel: string;
  destructive: boolean;
}

@Component({
  selector: 'app-service-cases-list',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    A11yModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTableModule,
  ],
  templateUrl: './service-cases-list.html',
  styleUrl: './service-cases-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceCasesList implements OnInit {
  private readonly api = inject(ServiceCasesApi);
  private readonly assetApi = inject(AssetApi);
  private readonly vendorApi = inject(VendorApiService);

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  @ViewChild('detailsDialog', { static: true })
  private detailsDialog!: TemplateRef<unknown>;

  @ViewChild('openDialog', { static: true })
  private openDialog!: TemplateRef<unknown>;

  @ViewChild('confirmationDialog', { static: true })
  private confirmationDialog!: TemplateRef<{
    $implicit: ConfirmationData;
  }>;

  private detailsDialogRef: MatDialogRef<unknown, void> | null = null;
  private openDialogRef: MatDialogRef<unknown, void> | null = null;
  private confirmationDialogRef:  MatDialogRef<unknown, boolean> | null = null;

  private detailsSubscription?: Subscription;
  private detailsSequence = 0;
  private listReloadRequested = false;
  private preselectedAssetId: string | null = null;
  private assetsLoaded = false;

  readonly items = signal<readonly ServiceCaseListItem[]>([]);
  readonly selectedId = signal<string | null>(null);
  readonly details = signal<ServiceCaseDetails | null>(null);

  readonly listLoading = signal(false);
  readonly detailsLoading = signal(false);
  readonly listFailed = signal(false);
  readonly detailsFailed = signal(false);

  readonly selectedAction = signal<ServiceCaseAction | null>(null);

  readonly actionComment = signal('');
  readonly actionVendorId = signal('');
  readonly actionInProgress = signal(false);

  readonly vendors = signal<readonly GetVendorsResponse[]>([]);
  readonly vendorsLoading = signal(false);
  readonly vendorsFailed = signal(false);

  readonly openInProgress = signal(false);
  readonly confirming = signal(false);

  readonly assets = signal<readonly GetAssetsResponse[]>([]);
  readonly assetsLoading = signal(false);
  readonly assetsFailed = signal(false);

  readonly assetSearchText = signal('');
  readonly selectedAsset = signal<GetAssetsResponse | null>(null);
  readonly assetPreselected = signal(false);

  readonly newCaseIssueType = signal<ServiceIssueType>(ServiceIssueType.Unknown);

  readonly newCaseOtherIssueDescription = signal('');
  readonly newCaseDescription = signal('');

  readonly selectedStatusFilters = signal<readonly ServiceCaseStatus[]>([]);

  readonly displayedColumns = [
    'caseNumber',
    'asset',
    'issueType',
    'status',
    'createdAt',
  ];
  readonly assetDisplayedColumns = [
    'select',
    'assetName',
    'serialNumber',
    'store',
    'assetStatus',
  ];

  readonly serviceIssueTypeOptions: readonly ServiceIssueType[] = [
    ServiceIssueType.DoesNotTurnOn,
    ServiceIssueType.PhysicalDamage,
    ServiceIssueType.ChargingIssue,
    ServiceIssueType.DisplayIssue,
    ServiceIssueType.NetworkIssue,
    ServiceIssueType.SoftwareIssue,
    ServiceIssueType.ContinuousRestart,
    ServiceIssueType.FaultyButton,
    ServiceIssueType.Other,
  ];

  readonly serviceCaseStatusOptions: readonly ServiceCaseStatus[] = [
    ServiceCaseStatus.AwaitingCentralReceipt,
    ServiceCaseStatus.UnderCentralInspection,
    ServiceCaseStatus.AwaitingExternalServiceDispatch,
    ServiceCaseStatus.SentToExternalService,
    ServiceCaseStatus.InExternalService,
    ServiceCaseStatus.AwaitingReturn,
    ServiceCaseStatus.ReturningToStore,
    ServiceCaseStatus.Resolved,
    ServiceCaseStatus.Closed,
    ServiceCaseStatus.Cancelled,
  ];

  private readonly packageManagedActions:
    readonly ServiceCaseAction[] = [
      'ReceiveAtHeadquarters',
      'DispatchToOriginStore',
      'ReceiveAtOriginStore',
    ];

  readonly getStatusLabel = getServiceCaseStatusLabel;
  readonly getIssueTypeLabel = getServiceIssueTypeLabel;
  readonly getActionLabel = getServiceCaseActionLabel;
  readonly getEventTypeLabel = getServiceCaseEventTypeLabel;

  readonly busy = computed(
    () =>
      this.openInProgress() ||
      this.actionInProgress() ||
      this.confirming(),
  );

  readonly assetsById = computed(() => new Map(this.assets().map(asset => [asset.assetId, asset])),
  );

  readonly filteredItems = computed(() => {
    const selectedStatuses = this.selectedStatusFilters();

    if (selectedStatuses.length === 0) {
      return this.items();
    }

    const selectedSet = new Set(selectedStatuses);

    return this.items().filter(item => selectedSet.has(this.normalizeStatus(item.status)));
  });

  readonly statusCounts = computed(() => {
    const counts = new Map<ServiceCaseStatus, number>();

    for (const item of this.items()) {
      const status = this.normalizeStatus(item.status);
      counts.set(status, (counts.get(status) ?? 0) + 1);
    }

    return counts;
  });

  readonly availableServiceCaseActions = computed(() => {
    const serviceCase = this.details();

    return serviceCase ? serviceCase.availableActions.filter(action => !this.isPackageManagedAction(action)) : [];
  });

  readonly filteredAssets = computed(() => {
    const search = this.normalizeSearchText(
      this.assetSearchText(),
    );

    const selectedAsset = this.selectedAsset();

    if (this.assetPreselected()) {
      return selectedAsset ? [selectedAsset] : [];
    }

    if (!search) {
      return this.assets();
    }

    return this.assets().filter(asset => {
      const searchableValues = [
        asset.assetName,
        asset.manufacturer,
        asset.model,
        asset.serialNumber,
        asset.assignedStoreNumber,
      ];

      return searchableValues.some(value =>
        this.normalizeSearchText(
          String(value ?? ''),
        ).includes(search),
      );
    });
  });

  readonly canOpenServiceCase = computed(
    () =>
      !this.busy() &&
      !this.assetsLoading() &&
      !this.assetsFailed() &&
      this.validateOpenForm() === null,
  );

  readonly canSubmitAction = computed(
    () =>
      !this.busy() &&
      !this.detailsLoading() &&
      this.validateAction() === null,
  );

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.detailsSequence++;
      this.detailsSubscription?.unsubscribe();
      this.confirmationDialogRef?.close(false);
      this.openDialogRef?.close();
      this.detailsDialogRef?.close();
    });
  }

  ngOnInit(): void {
    this.loadList();
    this.loadAssets();
    this.readAssetFromQueryParameters();
  }

  // List and details

  loadList(): void {
    if (this.destroyRef.destroyed) {
      return;
    }

    if (this.listLoading()) {
      this.listReloadRequested = true;
      return;
    }

    this.listReloadRequested = false;
    this.listLoading.set(true);
    this.listFailed.set(false);

    this.api.getList()
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
        next: response => {
          this.items.set(response.items);
        },
        error: (error: unknown) => {
          console.error('Failed to load service cases.', error);
          this.listFailed.set(true);
          this.showError('A szervizügyek betöltése nem sikerült.');
        },
      });
  }

  selectCase(id: string): void {
    if (this.busy() || this.openDialogRef || this.destroyRef.destroyed) {
      return;
    }

    if (this.detailsDialogRef) {
      return;
    }

    this.selectedId.set(id);
    this.details.set(null);
    this.resetActionForm();
    this.loadDetails(id);

    const ref = this.dialog.open<unknown, unknown, void>(
      this.detailsDialog,
      {
        width: '1100px',
        maxWidth: '96vw',
        maxHeight: '92dvh',
        ariaLabelledBy: 'service-case-details-title',
        autoFocus: 'first-tabbable',
        restoreFocus: true,
      },
    );

    this.detailsDialogRef = ref;

    ref.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.detailsDialogRef !== ref) {
          return;
        }

        this.detailsDialogRef = null;
        this.detailsSequence++;
        this.detailsSubscription?.unsubscribe();
        this.selectedId.set(null);
        this.details.set(null);
        this.detailsLoading.set(false);
        this.detailsFailed.set(false);
        this.resetActionForm();
      });
  }

  closeDetailsDialog(): void {
    if (!this.busy()) {
      this.detailsDialogRef?.close();
    }
  }

  reloadDetails(): void {
    const id = this.selectedId();

    if (id && !this.busy() && !this.detailsLoading()) {
      this.resetActionForm();
      this.loadDetails(id);
    }
  }

  private loadDetails(id: string): void {
    this.detailsSubscription?.unsubscribe();

    const sequence = ++this.detailsSequence;
    this.detailsLoading.set(true);
    this.detailsFailed.set(false);

    this.detailsSubscription = this.api.getDetails(id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          if (sequence === this.detailsSequence) {
            this.detailsLoading.set(false);
          }
        }),
      )
      .subscribe({
        next: response => {
          if (sequence === this.detailsSequence && this.selectedId() === id) {
            this.details.set(response);
          }
        },
        error: (error: unknown) => {
          if (sequence !== this.detailsSequence || this.selectedId() !== id) {
            return;
          }

          console.error('Failed to load service case details.', error);
          this.details.set(null);
          this.detailsFailed.set(true);
          this.showError('Az ügy részleteinek betöltése nem sikerült.');
        },
      });
  }

  changeStatusFilter(values: readonly ServiceCaseStatus[] | null | undefined): void {
    const validStatuses = (values ?? []).filter(status => this.serviceCaseStatusOptions.includes(status));

    this.selectedStatusFilters.set([...new Set(validStatuses)]);
  }

clearStatusFilters(): void {
  this.selectedStatusFilters.set([]);
}


  // Assets

  loadAssets(): void {
    if (this.assetsLoading() || this.destroyRef.destroyed) {
      return;
    }

    this.assetsLoading.set(true);
    this.assetsFailed.set(false);

    this.assetApi.getAll()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.assetsLoading.set(false)),
      )
      .subscribe({
        next: assets => {
          this.assets.set(
            [...assets].sort((left, right) =>
              left.assetName.localeCompare(
                right.assetName,
                'hu-HU',
                { sensitivity: 'base' },
              ),
            ),
          );

          this.assetsLoaded = true;
          this.resolvePreselectedAsset();
        },
        error: (error: unknown) => {
          console.error('Failed to load assets.', error);
          this.assetsFailed.set(true);
          this.showError('Az eszközök betöltése nem sikerült.');
        },
      });
  }

  getAssetById(id: string): GetAssetsResponse | null {
    return this.assetsById().get(id) ?? null;
  }

  updateAssetSearchText(value: string): void {
    if (this.assetPreselected() || this.busy() || this.assetsLoading()) {
      return;
    }

    this.assetSearchText.set(value ?? '');
  }

  selectAssetById(id: string): void {
    if (this.assetPreselected() || this.busy() || this.assetsLoading() || this.assetsFailed()) {
      return;
    }

    const asset = this.getAssetById(id);

    if (!asset) {
      return;
    }

    this.selectedAsset.set(asset);
  }

  clearSelectedAsset(): void {
    if (this.assetPreselected() || this.busy()) {
      return;
    }

    this.selectedAsset.set(null);
  }

  private normalizeSearchText(value: string): string {
    return value
      .normalize('NFD')
      .replace(/\p{Diacritic}/gu, '')
      .trim()
      .toLocaleLowerCase('hu-HU');
  }

  // Creation dialog

  showOpenForm(): void {
    if (this.busy() || this.openDialogRef || this.detailsDialogRef) {
      return;
    }

    this.resetOpenFormFields();
    this.preselectedAssetId = null;
    this.assetPreselected.set(false);
    this.openCreationDialog();

    if (!this.assetsLoaded && !this.assetsLoading()) {
      this.loadAssets();
    }
  }

  private openCreationDialog(): void {
    if (this.openDialogRef || this.destroyRef.destroyed) {
      return;
    }

    const ref = this.dialog.open<unknown, unknown, void>(
      this.openDialog,
      {
        width: '800px',
        maxWidth: '96vw',
        maxHeight: '92dvh',
        ariaLabelledBy: 'open-service-case-title',
        autoFocus: 'first-tabbable',
        restoreFocus: true,
      },
    );

    this.openDialogRef = ref;

    ref.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.openDialogRef !== ref) {
          return;
        }

        this.openDialogRef = null;
        this.preselectedAssetId = null;
        this.assetPreselected.set(false);
        this.resetOpenFormFields();
        void this.clearQueryParameters();
      });
  }

  cancelOpenForm(): void {
    if (!this.busy()) {
      this.openDialogRef?.close();
    }
  }

  isOtherIssueSelected(): boolean {
    return this.newCaseIssueType() === ServiceIssueType.Other;
  }

  openServiceCase(): void {
    if (this.busy()) {
      return;
    }

    const message = this.validateOpenForm();

    if (message) {
      this.showError(message);
      return;
    }

    const asset = this.selectedAsset();

    if (!asset) {
      return;
    }

    const request: OpenServiceCaseRequest = {
      assetId: asset.assetId,
      issueType: this.newCaseIssueType(),
      otherIssueDescription: this.isOtherIssueSelected()
        ? this.normalizeOptionalText(this.newCaseOtherIssueDescription())
        : null,
      description: this.newCaseDescription().trim(),
    };

    this.confirm({
      title: 'Szervizügy létrehozása',
      message: `Létrehozod a szervizügyet a(z) ${asset.assetName} eszközhöz?`,
      detail: request.description,
      confirmLabel: 'Létrehozás',
      destructive: false,
    }, () => {
      this.openInProgress.set(true);
      this.updateDialogLocks();

      this.api.open(request)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => {
            this.openInProgress.set(false);
            this.updateDialogLocks();
          }),
        )
        .subscribe({
          next: created => {
            this.showSuccess(`A(z) ${created.caseNumber} számú szervizügy létrejött.`);

            const ref = this.openDialogRef;

            if (ref) {
              // Open details only after the creation dialog closes.
              ref.afterClosed()
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe(() => {
                  if (!this.destroyRef.destroyed) {
                    this.selectCase(created.id);
                  }
                });

              ref.close();
            }

            this.loadList();
          },
          error: (error: unknown) => {
            console.error('Failed to open service case.', error);
            this.showError(this.getOperationError(error, 'open'));
          },
        });
    });
  }

  private validateOpenForm(): string | null {
    if (!this.selectedAsset()) {
      return 'Válassz egy eszközt.';
    }

    if (!this.serviceIssueTypeOptions.includes(this.newCaseIssueType())) {
      return 'Válassz hibatípust.';
    }

    const description = this.newCaseDescription().trim();

    if (!description || description.length > 2000) {
      return 'A hibaleírás kötelező, és legfeljebb 2000 karakter lehet.';
    }

    const other = this.newCaseOtherIssueDescription().trim();

    if (this.isOtherIssueSelected() && (!other || other.length > 2000)) {
      return 'Az egyéb hiba megnevezése kötelező, és legfeljebb 2000 karakter lehet.';
    }

    return null;
  }

  private resetOpenFormFields(): void {
    this.assetSearchText.set('');
    this.selectedAsset.set(null);
    this.newCaseIssueType.set(ServiceIssueType.Unknown);
    this.newCaseOtherIssueDescription.set('');
    this.newCaseDescription.set('');
  }

  private readAssetFromQueryParameters(): void {
    const assetId = this.route.snapshot.queryParamMap.get('assetId');
    const shouldOpen = this.route.snapshot.queryParamMap.get('open') === 'true';

    if (!shouldOpen || !assetId) {
      return;
    }

    this.preselectedAssetId = assetId;
    this.openCreationDialog();
    this.resolvePreselectedAsset();
  }

  private resolvePreselectedAsset(): void {
    if (!this.preselectedAssetId || !this.assetsLoaded) {
      return;
    }

    const asset = this.getAssetById(this.preselectedAssetId);

    if (!asset) {
      this.preselectedAssetId = null;
      this.assetPreselected.set(false);
      this.selectedAsset.set(null);

      this.showError('Az eszközrészletezőből kiválasztott eszköz nem található vagy nem érhető el.');

      return;
    }

    this.selectedAsset.set(asset);
    this.assetSearchText.set('');
    this.assetPreselected.set(true);
  }

  private clearQueryParameters(): Promise<boolean> {
    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { assetId: null, open: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  // Actions

  isPackageManagedAction(action: ServiceCaseAction): boolean {
    return this.packageManagedActions.includes(action);
  }

  selectAction(action: ServiceCaseAction): void {
    if (this.busy() || !this.availableServiceCaseActions().includes(action)) {
      return;
    }

    this.resetActionForm();
    this.selectedAction.set(action);

    if (this.requiresVendor(action) && this.vendors().length === 0) {
      this.loadVendors();
    }
  }

  cancelAction(): void {
    if (!this.busy()) {
      this.resetActionForm();
    }
  }

  requiresVendor(action: ServiceCaseAction): boolean {
    return action === 'RequireExternalService';
  }

  private validateAction(): string | null {
    const action = this.selectedAction();
    const item = this.details();

    if (
      !action ||
      !item ||
      this.selectedId() !== item.id ||
      !item.availableActions.includes(action) ||
      this.isPackageManagedAction(action)) {
      return 'A kiválasztott művelet nem érhető el.';
    }

    if (this.requiresVendor(action) &&
      (
        this.vendorsLoading() ||
        this.vendorsFailed() ||
        !this.vendors().some(vendor => vendor.id === this.actionVendorId()))) {
      return 'Válassz egy elérhető szervizpartnert.';
    }

    const comment = this.actionComment().trim();

    if (action === 'DisposeAsset' && !comment) {
      return 'A selejtezés indoklása kötelező.';
    }

    if (comment.length > 2000) {
      return 'A megjegyzés legfeljebb 2000 karakter lehet.';
    }

    return null;
  }

  executeSelectedAction(): void {
    if (this.busy()) {
      return;
    }

    const message = this.validateAction();

    if (message) {
      this.showError(message);
      return;
    }

    const item = this.details();
    const action = this.selectedAction();

    if (!item || !action) {
      return;
    }

    const caseId = item.id;
    const comment = this.actionComment().trim();
    const vendorId = this.actionVendorId();

    this.confirm({
      title: this.getActionLabel(action),
      message: action === 'DisposeAsset'
        ? 'Az eszköz selejtezett, a szervizügy megoldott állapotba kerül. A végleges lezárás külön művelet. Megerősíted a selejtezést?'
        : action === 'Close'
          ? 'Megerősíted a szervizügy végleges lezárását?'
          : `Végrehajtod a(z) „${this.getActionLabel(action)}” műveletet a(z) ${item.caseNumber} ügyön?`,
      detail: comment || null,
      confirmLabel: 'Megerősítés',
      destructive: action === 'DisposeAsset',
    }, () => {
      if (
        this.selectedId() !== caseId ||
        this.details()?.id !== caseId ||
        !this.details()?.availableActions.includes(action) ||
        this.isPackageManagedAction(action)
      ) {
        this.showError('Az ügy adatai megváltoztak. Frissítsd a részleteket.');
        return;
      }

      const operation = this.createActionRequest(caseId, action, comment, vendorId);

      if (!operation) {
        this.showError('A művelet nem hajtható végre ezen a felületen.');
        return;
      }

      this.actionInProgress.set(true);
      this.updateDialogLocks();

      operation
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => {
            this.actionInProgress.set(false);
            this.updateDialogLocks();
          }),
        )
        .subscribe({
          next: updated => {
            this.details.set(updated);
            this.resetActionForm();
            this.showSuccess(`${this.getActionLabel(action)}: sikeres művelet.`);

            this.loadList();
            this.loadAssets();
          },
          error: (error: unknown) => {
            console.error('Failed to execute service case action.', error);
            this.showError(this.getOperationError(error, 'action'));
          },
        });
    });
  }

  private createActionRequest(caseId: string, action: ServiceCaseAction, comment: string, vendorId: string): Observable<ServiceCaseDetails> | null {
    if (action === 'RequireExternalService') {
      const request: RequireExternalServiceRequest = {
        vendorId,
        comment: this.normalizeOptionalText(comment),
      };

      return this.api.requireExternalService(caseId, request);
    }

    if (action === 'DisposeAsset') {
      const request: DisposeServiceCaseAssetRequest = {
        reason: comment,
      };

      return this.api.disposeAsset(caseId, request);
    }

    const request: ServiceCaseCommentRequest = {
      comment: this.normalizeOptionalText(comment),
    };

    switch (action) {
      case 'DispatchToExternalService':
        return this.api.dispatchToExternalService(caseId, request);

      case 'HandOverToExternalService':
        return this.api.handOverToExternalService(caseId, request);

      case 'ConfirmReceivedByExternalService':
        return this.api.confirmReceivedByExternalService(caseId, request);

      case 'ReturnFromExternalService':
        return this.api.returnFromExternalService(caseId, request);

      case 'CompleteRepairAtHeadquarters':
        return this.api.completeRepairAtHeadquarters(caseId, request);

      case 'ResolveAfterExternalServiceReturn':
        return this.api.resolveAfterExternalServiceReturn(caseId, request);

      case 'Close':
        return this.api.close(caseId, request);

      default:
        return null;
    }
  }

  private resetActionForm(): void {
    this.selectedAction.set(null);
    this.actionComment.set('');
    this.actionVendorId.set('');
  }

  loadVendors(): void {
    if (this.vendorsLoading() || this.destroyRef.destroyed) {
      return;
    }

    this.vendorsLoading.set(true);
    this.vendorsFailed.set(false);

    this.vendorApi.getAll()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.vendorsLoading.set(false)),
      )
      .subscribe({
        next: vendors => {
          this.vendors.set(
            [...vendors].sort((left, right) =>
              left.name.localeCompare(
                right.name,
                'hu-HU',
                { sensitivity: 'base' },
              ),
            ),
          );
        },
        error: (error: unknown) => {
          console.error('Failed to load vendors.', error);
          this.vendorsFailed.set(true);
          this.showError('A szervizpartnerek betöltése nem sikerült.');
        },
      });
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

  // Dialogs and notifications

  private confirm(data: ConfirmationData, onConfirmed: () => void): void {
    if (this.busy() || this.destroyRef.destroyed) {
      return;
    }

    this.confirming.set(true);
    this.updateDialogLocks();

    const ref = this.dialog.open<unknown, ConfirmationData, boolean>(this.confirmationDialog, {
      data,
      width: '500px',
      maxWidth: '94vw',
      maxHeight: '90dvh',
      ariaLabelledBy: 'service-case-confirmation-title',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
    });

    this.confirmationDialogRef = ref;

    ref.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        this.confirmationDialogRef = null;
        this.confirming.set(false);
        this.updateDialogLocks();

        if (confirmed === true && !this.destroyRef.destroyed) {
          onConfirmed();
        }
      });
  }

  private updateDialogLocks(): void {
    const locked = this.busy();

    if (this.detailsDialogRef) {
      this.detailsDialogRef.disableClose = locked;
    }

    if (this.openDialogRef) {
      this.openDialogRef.disableClose = locked;
    }
  }

  private showSuccess(message: string): void {
    this.showSnackbar(message, false);
  }

  private showError(message: string): void {
    this.showSnackbar(message, true);
  }

  private showSnackbar(message: string, error: boolean): void {
    if (this.destroyRef.destroyed) {
      return;
    }

    this.snackBar.open(message, 'Bezárás', {
      duration: error ? 7000 : 4000,
      horizontalPosition: 'end',
      verticalPosition: 'bottom',
      politeness: error ? 'assertive' : 'polite',
      panelClass: [
        'asset-management-snackbar',
        error
          ? 'asset-management-error'
          : 'asset-management-success',
      ],
    });
  }

  private getOperationError(error: unknown, operation: 'open' | 'action'): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 0:
          return 'A művelet eredménye nem ellenőrizhető. Újabb próbálkozás előtt frissítsd az ügyek adatait.';

        case 401:
          return 'A munkamenet lejárt. Jelentkezz be újra.';

        case 403:
          return 'Nincs jogosultságod a művelet végrehajtásához.';

        case 404:
          return 'Az ügy, az eszköz vagy a kapcsolódó adat nem található.';

        case 400:
        case 422:
          return 'A megadott adatok érvénytelenek. Ellenőrizd a kötelező mezőket.';

        case 409:
          return operation === 'open'
            ? 'Az eszközhöz már tartozhat aktív ügy vagy aktív csomag, illetve az állapota nem teszi lehetővé az ügyindítást.'
            : 'A művelet az ügy vagy az eszköz aktuális állapotával ütközik. Aktív csomagban lévő eszköz nem selejtezhető. Frissítsd az adatokat.';
      }
    }

    return operation === 'open'
      ? 'A szervizügy létrehozása nem sikerült.'
      : 'A művelet végrehajtása nem sikerült.';
  }

  private normalizeOptionalText(value: string): string | null {
    return value.trim() || null;
  }

  private normalizeStatus(value: ServiceCaseStatusValue): ServiceCaseStatus {
    return typeof value === 'number' ? value : ServiceCaseStatus[value];
  }

  getStatusTone(status: ServiceCaseStatusValue): string {
    switch (this.normalizeStatus(status)) {
      case ServiceCaseStatus.AwaitingCentralReceipt:
        return 'amber';

      case ServiceCaseStatus.UnderCentralInspection:
        return 'blue';

      case ServiceCaseStatus.AwaitingExternalServiceDispatch:
        return 'orange';

      case ServiceCaseStatus.SentToExternalService:
        return 'cyan';

      case ServiceCaseStatus.InExternalService:
        return 'violet';

      case ServiceCaseStatus.AwaitingReturn:
        return 'indigo';

      case ServiceCaseStatus.ReturningToStore:
        return 'blue';

      case ServiceCaseStatus.Resolved:
        return 'green';

      case ServiceCaseStatus.Cancelled:
        return 'red';

      default:
        return 'slate';
    }
  }

  toUtcDate(value: string): string {
    return /(?:z|[+-]\d{2}:?\d{2})$/i.test(value) ? value : `${value}Z`;
  }
}