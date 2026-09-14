import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AssetApi } from '../data-access/asset-api';
import { ASSET_TYPE, ASSET_TYPE_LABELS, AssetType } from '../data-access/models/asset-type';
import { ASSET_STATUS_LABELS, ASSET_STATUSES, AssetStatus } from '../data-access/models/asset-status';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateAssetRequest } from '../data-access/models/create-asset-request';
import { GetStoresResponse } from '../../stores/data-access/models/get-stores-response';
import { StoreApiService } from '../../stores/data-access/store-api';
import { NotificationService } from '../../../core/notifications/notification-service';

const MAC_ADDRESS_PATTERN = /^$|^([0-9A-F]{2}:){5}[0-9A-F]{2}$/;

@Component({
  selector: 'app-asset-create',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './asset-create.html',
  styleUrl: './asset-create.css',
})


export class AssetCreate implements OnInit {
  private readonly assetApi = inject(AssetApi);
  private readonly router = inject(Router);
  private readonly notificationService = inject(NotificationService);
  readonly assetTypes = ASSET_TYPE;
  readonly assetStatuses = ASSET_STATUSES;

  readonly typeLabels = ASSET_TYPE_LABELS;
  readonly statusLabels = ASSET_STATUS_LABELS;

  private readonly storeApi = inject(StoreApiService);
  readonly storesLoading = signal(true);
  readonly storesError = signal<string | null>(null);
  readonly stores = signal<readonly GetStoresResponse[]>([]);

  readonly isSaving = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly form = new FormGroup({
    assetName: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(200)
      ]
    }),
    assetType: new FormControl<AssetType | ''>('',
      {
        nonNullable: true,
        validators: [Validators.required]
      }),
    assetStatus: new FormControl<AssetStatus >('Unknown',
      {
        nonNullable: true,
        validators: [Validators.required]
      }),
    manufacturer: new FormControl('',
      {
        nonNullable: true,
        validators: [
          Validators.required, Validators.maxLength(200)
        ]
      }),
    model: new FormControl('',
      {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(200)]
      }),
    serialNumber: new FormControl('',
      {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(200)]
      }),
    rfidTagId: new FormControl('',
      {
        nonNullable: true,
        validators: [Validators.maxLength(200)]
      }),
    macAddress: new FormControl('',
      {
        nonNullable: true,
        validators: [Validators.pattern(MAC_ADDRESS_PATTERN)]
      }),
    wifiMacAddress: new FormControl('',
      {
        nonNullable: true,
        validators: [Validators.pattern(MAC_ADDRESS_PATTERN)]
      }),
    imei: new FormControl('', { nonNullable: true }),
    operatingSystem: new FormControl('', { nonNullable: true }),
    operatingSystemVersion: new FormControl('', { nonNullable: true }),
    assignedStoreId: new FormControl('',
      {
        nonNullable: true,
        validators: [
          Validators.required,
        ]
      }),
    assignedUserId: new FormControl('',
      {
        nonNullable: true,
        validators: [
          Validators.pattern(
            /^[0-9a-fA-F]{8} - [0-9a-fA-F]{4} - [1-5][0-9a-fA-F]{3} - [89abAB][0-9a-fA-F]{3} - [0-9a-fA-F]{12}$ /
          )
        ]
      })
  });

  ngOnInit(): void {
    this.loadStores();
  }

  save(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      this.notificationService.error('Az eszköz mentése sikertelen. Kérlek ellenőrizd az adatokat.');
      return;
    }
    const value = this.form.getRawValue();

    if (!value.assetType) {
      return;
    }

    const request: CreateAssetRequest = {
      assetName: value.assetName.trim(),
      assetType: value.assetType,
      assetStatus: value.assetStatus,
      manufacturer: value.manufacturer,
      model: value.model,
      serialNumber: value.serialNumber,
      macAddress: this.toNullable(value.macAddress),
      wifiMacAddress: this.toNullable(value.wifiMacAddress),
      imei: this.toNullable(value.imei),
      operatingSystem: this.toNullable(value.operatingSystem),
      operatingSystemVersion: this.toNullable(value.operatingSystemVersion),
      assignedStoreId: value.assignedStoreId,
      assignedUserId: this.toNullable(value.assignedUserId),
      rfidTagId: this.toNullable(value.rfidTagId)
    };

    this.isSaving.set(true);
    this.saveError.set(null);

    this.assetApi.create(request).subscribe(
      {
        next: () => {
          void this.router.navigate(['/assets']);
          this.notificationService.success('Az eszköz sikeresen létrehozva.');
        },
        error: error => {
          console.error("Az eszköz létrehozása sikertelen", error);
          this.notificationService.error('Az eszköz mentése sikertelen. Kérlek ellenőrizd az adatokat.');
          this.saveError.set("Az eszköz mentése sikertelen. Kérlek ellenőrizd az adatokat.");
          this.isSaving.set(false);
        }
      }
    )
  }

  private toNullable(value: string): string | null {
    const trimmedValue = value.trim();
    return trimmedValue.length > 0 ? trimmedValue : null;
  };

  formatMacAddress(controlName: 'macAddress' | 'wifiMacAddress', event: Event): void {
    const input = event.target as HTMLInputElement;
    const hexadecimalCharacters = input.value
      .toUpperCase()
      .replace(/[^0-9A-F]/g, '')
      .slice(0, 12);

    const formattedValue = hexadecimalCharacters
      .match(/.{1,2}/g)
      ?.join(':')
      ?? '';

    input.value = formattedValue;

    this.form.controls[controlName].setValue(formattedValue, { emitEvent: false })
    this.form.controls[controlName].markAsDirty();
  }

  loadStores(): void {
    this.storesLoading.set(true);
    this.storesError.set(null);

    this.storeApi.getAll().subscribe({
      next: stores => {
        const sortedStores = [...stores].sort((a, b) => parseInt(a.storeNumber, 10) - parseInt(b.storeNumber, 10));
        this.stores.set(sortedStores);
        this.storesLoading.set(false);
      },
      error: error => {
        console.error("Az áruházlista betöltése sikertelen.", error);
        this.storesError.set("Az áruházakat jelenleg nem lehet betölteni.");
        this.storesLoading.set(false);
      }
    })
  }
}
