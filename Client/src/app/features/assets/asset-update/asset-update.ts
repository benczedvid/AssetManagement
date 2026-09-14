import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';

import {
  ActivatedRoute,
  Router,
  RouterLink,
} from '@angular/router';

import { finalize } from 'rxjs';

import { NotificationService } from
  '../../../core/notifications/notification-service';

import { CurrentUserApi } from
  '../../auth/current-user-api';

import { GetStoresResponse } from
  '../../stores/data-access/models/get-stores-response';

import { StoreApiService } from
  '../../stores/data-access/store-api';

import { GetVendorsResponse } from
  '../../vendors/data-access/models/get-vendors-response';

import { VendorApiService } from
  '../../vendors/data-access/vendor-api';

import { AssetApi } from
  '../data-access/asset-api';

import {
  ASSET_STATUS_LABELS,
  ASSET_STATUSES,
  AssetStatus,
} from
  '../data-access/models/asset-status';

import {
  ASSET_TYPE,
  ASSET_TYPE_LABELS,
  AssetType,
} from
  '../data-access/models/asset-type';

import { UpdateAssetRequest } from
  '../data-access/models/update-asset-request';

@Component({
  selector: 'app-asset-update',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './asset-update.html',
  styleUrl: './asset-update.css',
})
export class AssetUpdate implements OnInit {
  private readonly formBuilder =
    inject(FormBuilder);

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly assetApi =
    inject(AssetApi);

  private readonly notificationService =
    inject(NotificationService);

  private readonly currentUserApi =
    inject(CurrentUserApi);

  private readonly storeApi =
    inject(StoreApiService);

  private readonly vendorApi =
    inject(VendorApiService);

  readonly assetStatuses =
    ASSET_STATUSES;

  readonly assetStatusLabels =
    ASSET_STATUS_LABELS;

  readonly assetTypes =
    ASSET_TYPE;

  readonly assetTypeLabels =
    ASSET_TYPE_LABELS;

  readonly storesLoading =
    signal(false);

  readonly storesError =
    signal<string | null>(null);

  readonly stores =
    signal<readonly GetStoresResponse[]>([]);

  readonly vendorsLoading =
    signal(false);

  readonly vendorsError =
    signal<string | null>(null);

  readonly vendors =
    signal<readonly GetVendorsResponse[]>([]);

  readonly assetId =
    signal<string | null>(null);

  readonly isLoading =
    signal(false);

  readonly isSaving =
    signal(false);

  readonly loadError =
    signal<string | null>(null);

  readonly canManageAssets = computed(
    () =>
      this.currentUserApi.canManageAssets()
  );

  readonly assetForm =
    this.formBuilder.nonNullable.group({
      assetName: [
        '',
        [
          Validators.required,
          Validators.maxLength(200),
        ],
      ],

      assetStatus:
        this.formBuilder.nonNullable.control<AssetStatus>(
          'Unknown',
          {
            validators: [
              Validators.required,
            ],
          }
        ),

      assetType:
        this.formBuilder.nonNullable.control<AssetType>(
          'Unknown',
          {
            validators: [
              Validators.required,
            ],
          }
        ),

      manufacturer: [
        '',
        [
          Validators.maxLength(100),
        ],
      ],

      model: [
        '',
        [
          Validators.maxLength(100),
        ],
      ],

      serialNumber: [
        '',
        [
          Validators.required,
          Validators.maxLength(100),
        ],
      ],

      macAddress: [
        '',
        [
          Validators.maxLength(17),
        ],
      ],

      wifiMacAddress: [
        '',
        [
          Validators.maxLength(17),
        ],
      ],

      imei: [
        '',
        [
          Validators.maxLength(20),
        ],
      ],

      storeId: [
        '',
      ],

      operatingSystem: [
        '',
        [
          Validators.maxLength(100),
        ],
      ],

      operatingSystemVersion: [
        '',
        [
          Validators.maxLength(100),
        ],
      ],

      assignedVendorId:
        this.formBuilder.nonNullable.control<string>(
          ''
        ),

      rfidTagId: [
        '',
        [
          Validators.maxLength(200),
        ],
      ],
    });

  ngOnInit(): void {
    if (!this.canManageAssets()) {
      void this.router.navigate(
        [
          '/forbidden',
        ],
        {
          replaceUrl: true,
        }
      );

      return;
    }

    this.loadStores();
    this.loadAsset();
    this.loadVendors();
  }

  loadAsset(): void {
    const id =
      this.route.snapshot.paramMap.get('id');

    if (!id) {
      const message =
        'The asset identifier is missing.';

      this.loadError.set(message);

      this.notificationService.error(
        'Az eszköz azonosítója hiányzik, ezért nem lehet betölteni az eszközt.'
      );

      return;
    }

    this.assetId.set(id);
    this.isLoading.set(true);
    this.loadError.set(null);

    this.assetApi
      .getDetails(id)
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        })
      )
      .subscribe({
        next: asset => {
          this.assetForm.reset({
            assetName:
              asset.assetName ?? '',

            assetStatus:
              asset.assetStatus ?? 'Unknown',

            assetType:
              asset.assetType ?? 'Unknown',

            manufacturer:
              asset.manufacturer ?? '',

            model:
              asset.model ?? '',

            serialNumber:
              asset.serialNumber ?? '',

            macAddress:
              asset.macAddress ?? '',

            wifiMacAddress:
              asset.wifiMacAddress ?? '',

            imei:
              asset.imei ?? '',

            storeId:
              asset.assignedStoreId ?? '',

            operatingSystem:
              asset.operatingSystem ?? '',

            operatingSystemVersion:
              asset.operatingSystemVersion ?? '',

            assignedVendorId:
              asset.assignedVendorId ?? '',

            rfidTagId:
              asset.rfidTagId ?? '',
          });
        },

        error: error => {
          console.error(
            'The asset could not be loaded.',
            error
          );

          const message =
            this.getErrorMessage(
              error,
              'The asset could not be loaded.'
            );

          this.loadError.set(message);

          this.notificationService.error(
            message
          );
        },
      });
  }

  save(): void {
    if (!this.canManageAssets()) {
      this.notificationService.error(
        'Nincs jogosultságod az eszköz módosításához.'
      );

      void this.router.navigate(
        [
          '/forbidden',
        ],
        {
          replaceUrl: true,
        }
      );

      return;
    }

    if (
      this.assetForm.invalid ||
      this.isSaving()
    ) {
      this.assetForm.markAllAsTouched();

      if (this.assetForm.invalid) {
        this.notificationService.warning(
          'Kérjük, korrigálja a validációs hibákat a mentés előtt.'
        );
      }

      return;
    }

    const id =
      this.assetId();

    if (!id) {
      this.notificationService.error(
        'Az eszköz azonosítója hiányzik, ezért nem lehet frissíteni az eszközt.'
      );

      return;
    }

    const formValue =
      this.assetForm.getRawValue();

    const request: UpdateAssetRequest = {
      assetName:
        formValue.assetName.trim(),

      assetStatus:
        formValue.assetStatus,

      assetType:
        formValue.assetType,

      manufacturer:
        this.getOptionalValue(
          formValue.manufacturer
        ),

      model:
        this.getOptionalValue(
          formValue.model
        ),

      serialNumber:
        formValue.serialNumber.trim(),

      macAddress:
        this.getOptionalValue(
          formValue.macAddress
        ),

      wifiMacAddress:
        this.getOptionalValue(
          formValue.wifiMacAddress
        ),

      imei:
        this.getOptionalValue(
          formValue.imei
        ),

      assignedStoreId:
        this.getOptionalValue(
          formValue.storeId
        ),

      operatingSystem:
        this.getOptionalValue(
          formValue.operatingSystem
        ),

      operatingSystemVersion:
        this.getOptionalValue(
          formValue.operatingSystemVersion
        ),

      assignedVendorId:
        this.getOptionalValue(
          formValue.assignedVendorId
        ),

      rfidTagId:
        this.getOptionalValue(
          formValue.rfidTagId
        ),
    };

    this.isSaving.set(true);

    this.assetApi
      .update(
        id,
        request
      )
      .pipe(
        finalize(() => {
          this.isSaving.set(false);
        })
      )
      .subscribe({
        next: () => {
          this.notificationService.success(
            'Az eszköz sikeresen frissítve.'
          );

          void this.router.navigate([
            '/assets',
            id,
          ]);
        },

        error: error => {
          console.error(
            'The asset could not be updated.',
            error
          );

          const message =
            this.getErrorMessage(
              error,
              'Az eszköz frissítése sikertelen.'
            );

          this.notificationService.error(
            message
          );
        },
      });
  }

  cancel(): void {
    const id =
      this.assetId();

    if (id) {
      void this.router.navigate([
        '/assets',
        id,
      ]);

      return;
    }

    void this.router.navigate([
      '/assets',
    ]);
  }

  hasError(
    controlName:
      keyof typeof this.assetForm.controls,
    errorName: string
  ): boolean {
    const control =
      this.assetForm.controls[controlName];

    return (
      control.touched &&
      control.hasError(errorName)
    );
  }

  loadStores(): void {
    if (this.storesLoading()) {
      return;
    }

    this.storesLoading.set(true);
    this.storesError.set(null);

    this.storeApi
      .getAll()
      .pipe(
        finalize(() => {
          this.storesLoading.set(false);
        })
      )
      .subscribe({
        next: stores => {
          const sortedStores = [
            ...stores,
          ].sort(
            (
              firstStore,
              secondStore
            ) =>
              String(
                firstStore.storeNumber
              ).localeCompare(
                String(
                  secondStore.storeNumber
                ),
                'hu',
                {
                  numeric: true,
                  sensitivity: 'base',
                }
              )
          );

          this.stores.set(
            sortedStores
          );
        },

        error: error => {
          console.error(
            'The store list could not be loaded.',
            error
          );

          const message =
            'The store list could not be loaded.';

          this.storesError.set(message);

          this.notificationService.error(
            message
          );
        },
      });
  }

  loadVendors(): void {
    if (this.vendorsLoading()) {
      return;
    }

    this.vendorsLoading.set(true);
    this.vendorsError.set(null);

    this.vendorApi
      .getAll()
      .pipe(
        finalize(() => {
          this.vendorsLoading.set(false);
        })
      )
      .subscribe({
        next: vendors => {
          const sortedVendors = [
            ...vendors,
          ].sort(
            (
              firstVendor,
              secondVendor
            ) =>
              String(
                firstVendor.name ?? ''
              ).localeCompare(
                String(
                  secondVendor.name ?? ''
                ),
                'hu',
                {
                  numeric: true,
                  sensitivity: 'base',
                }
              )
          );

          this.vendors.set(
            sortedVendors
          );
        },

        error: error => {
          console.error(
            'The vendor list could not be loaded.',
            error
          );

          const message =
            'The vendor list could not be loaded.';

          this.vendorsError.set(message);

          this.notificationService.error(
            message
          );
        },
      });
  }

  private getOptionalValue(
    value: string
  ): string | null {
    const trimmedValue =
      value.trim();

    return trimmedValue.length > 0
      ? trimmedValue
      : null;
  }

  private getErrorMessage(
    error: {
      status?: number;
      error?: {
        detail?: string;
      };
    },
    fallbackMessage: string
  ): string {
    const apiDetail =
      error.error?.detail;

    if (
      typeof apiDetail === 'string' &&
      apiDetail.trim().length > 0
    ) {
      return apiDetail;
    }

    switch (error.status) {
      case 400:
        return (
          'The submitted asset data ' +
          'is invalid.'
        );

      case 401:
        return (
          'Authentication is required ' +
          'to update the asset.'
        );

      case 403:
        return (
          'The current user does not have ' +
          'permission to update assets.'
        );

      case 404:
        return (
          'The requested asset could not ' +
          'be found.'
        );

      case 409:
        return (
          'Another asset already uses one ' +
          'of the submitted unique identifiers.'
        );

      case 0:
        return (
          'The AssetManagement API ' +
          'is unavailable.'
        );

      default:
        return fallbackMessage;
    }
  }
}