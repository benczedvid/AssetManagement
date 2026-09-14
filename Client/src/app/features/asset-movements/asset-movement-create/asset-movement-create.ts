import {
  Component,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { finalize } from 'rxjs';

import { AssetMovementApiService } from '../data-access/asset-movement-api';
import { CreateAssetMovementRequest } from '../data-access/models/create-asset-movement-request';
import { NotificationService } from '../../../core/notifications/notification-service';

@Component({
  selector: 'app-asset-movement-create',
  imports: [ReactiveFormsModule],
  templateUrl: './asset-movement-create.html',
  styleUrl: './asset-movement-create.css',
})
export class AssetMovementCreate {
  private readonly assetMovementApi = inject(AssetMovementApiService);
  private readonly notificationService = inject(NotificationService);

  private readonly employeeNumberInput = viewChild.required<ElementRef<HTMLInputElement>>('employeeNumberInput');

  private readonly rfidTagIdInput = viewChild.required<ElementRef<HTMLInputElement>>('rfidTagIdInput');

  readonly isSaving = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly form = new FormGroup({
    employeeNumber: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(10),
      ],
    }),

    rfidTagId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  employeeNumberCompleted(): void {
    const employeeNumberControl =
      this.form.controls.employeeNumber;

    employeeNumberControl.setValue(
      employeeNumberControl.value.trim()
    );

    employeeNumberControl.markAsTouched();

    if (employeeNumberControl.invalid) {
      employeeNumberControl.markAsDirty();
      return;
    }

    this.rfidTagIdInput().nativeElement.focus();
  }

  rfidTagIdCompleted(): void {
    const rfidTagIdControl =
      this.form.controls.rfidTagId;

    rfidTagIdControl.setValue(
      rfidTagIdControl.value.trim()
    );

    rfidTagIdControl.markAsTouched();

    if (
      rfidTagIdControl.invalid ||
      this.form.invalid ||
      this.isSaving()
    ) {
      this.form.markAllAsTouched();
      return;
    }

    this.save();
  }

  save(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const request: CreateAssetMovementRequest = {
      rfidTagId: value.rfidTagId.trim(),
      employeeNumber: value.employeeNumber.trim(),
    };

    this.isSaving.set(true);
    this.saveError.set(null);
    this.successMessage.set(null);

    this.assetMovementApi
      .create(request)
      .pipe(
        finalize(() => this.isSaving.set(false))
      )
      .subscribe({
        next: response => {
          const movementName =
            response.type === 'Checkout'
              ? 'Eszközfelvétel'
              : 'Eszközletétel';

          this.successMessage.set(
            `${movementName} sikeresen rögzítve.`
          );
          this.notificationService.success('Az eszközmozgás sikeresen rögzítve.');
          this.form.reset();

          queueMicrotask(() => {
            this.employeeNumberInput()
              .nativeElement
              .focus();
          });
        },

        error: error => {
          console.error(
            'Az eszközmozgás rögzítése sikertelen.',
            error
          );
          this.notificationService.error('Az eszközmozgás mentése sikertelen. Kérlek ellenőrizd az adatokat.');
          this.saveError.set(
            this.getErrorMessage(error.status)
          );
          this.form.reset();
          queueMicrotask(() => {
            this.employeeNumberInput()
              .nativeElement
              .focus();
          });
        },
      });
  }

  private getErrorMessage(status: number): string {
    switch (status) {
      case 400:
        return 'A megadott adatok érvénytelenek.';

      case 404:
        return 'A dolgozó vagy az eszköz nem található.';

      case 409:
        return 'Az eszköz a jelenlegi állapotában nem mozgatható.';

      default:
        return 'Az eszközmozgás mentése sikertelen.';
    }
  }
}
