import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ServiceDeskApi } from '../data-access/service-desk-api';
import { ServiceDeskSyncOperationItem, ServiceDeskSyncOperationStatus, ServiceDeskSyncOperationType } from '../models/get-service-desk-sync-operations-response';
import { ServiceDeskReconciliationOperation, ServiceDeskReconciliationOutcome, ResolveServiceDeskReconciliationRequest } from '../models/resolve-service-desk-reconciliation-request';

@Component({
  selector: 'app-service-desk-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatSnackBarModule,
  ],
  templateUrl: './service-desk-dashboard.html',
  styleUrl: './service-desk-dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceDeskDashboard implements OnInit {
  private readonly api = inject(ServiceDeskApi);
  private readonly snackBar = inject(MatSnackBar);

  // Loading states
  readonly loading = signal(false);
  readonly reconciling = signal(false);

  // Data
  readonly operations = signal<ServiceDeskSyncOperationItem[]>([]);
  readonly selectedOperation = signal<ServiceDeskSyncOperationItem | null>(null);

  // Table columns
  readonly displayedColumns = [
    'operationType',
    'status',
    'serviceCaseId',
    'createdAtUtc',
    'actions',
  ];

  // Operation type options
  readonly operationTypes: {
    value: ServiceDeskSyncOperationType | '';
    label: string;
  }[] = [
    {
      value: '',
      label: 'Összes művelet',
    },
    {
      value: 'RequestCreation',
      label: 'Hibajegy létrehozása',
    },
    {
      value: 'NoteCreation',
      label: 'Megjegyzés létrehozása',
    },
    {
      value: 'RequestClosure',
      label: 'Hibajegy lezárása',
    },
  ];

  // Status options
  readonly statuses: {
    value: ServiceDeskSyncOperationStatus | '';
    label: string;
  }[] = [
    {
      value: '',
      label: 'Összes állapot',
    },
    {
      value: 'Pending',
      label: 'Várakozik',
    },
    {
      value: 'Processing',
      label: 'Feldolgozás alatt',
    },
    {
      value: 'Completed',
      label: 'Sikeres',
    },
    {
      value: 'Failed',
      label: 'Sikertelen',
    },
    {
      value: 'RequiresReconciliation',
      label: 'Helyreállítást igényel',
    },
  ];

  // Reactive filters
  readonly searchText = signal('');
  readonly selectedStatus = signal<ServiceDeskSyncOperationStatus | ''>('');
  readonly selectedOperationType = signal<ServiceDeskSyncOperationType | ''>('');

  // Reconciliation form
  reconciliationOutcome = ServiceDeskReconciliationOutcome.Completed;

  verifiedRequestId = '';
  verificationComment = '';

  // Statistics
  readonly totalCount = computed(() => this.operations().length);
  readonly pendingCount = computed(() => this.operations().filter(operation => operation.status === 'Pending').length);
  readonly processingCount = computed(() => this.operations().filter(operation => operation.status === 'Processing').length);
  readonly completedCount = computed(() => this.operations().filter(operation => operation.status === 'Completed').length);
  readonly failedCount = computed(() => this.operations().filter(operation => operation.status === 'Failed').length);
  readonly reconciliationCount = computed(() => this.operations().filter(operation => operation.status === 'RequiresReconciliation').length);

  // Reactive filtering
  readonly filteredOperations = computed(() => {
    const search = this.searchText()
      .trim()
      .toLowerCase();

    const status = this.selectedStatus();
    const operationType = this.selectedOperationType();

    return this.operations().filter(operation => {
      const matchesStatus = !status || operation.status === status;
      const matchesType = !operationType || operation.operationType === operationType;

      const matchesSearch =
        !search ||
        operation.serviceCaseId
          .toLowerCase()
          .includes(search) ||
        operation.outboxId
          .toLowerCase()
          .includes(search) ||
        (operation.externalIdentifier ?? '')
          .toLowerCase()
          .includes(search);

      return (matchesStatus && matchesType && matchesSearch);
    });
  });

  ngOnInit(): void {
    this.loadOperations();
  }

  /**
   * Loads ServiceDesk synchronization operations.
   */
  loadOperations(): void {
    if (this.loading()) {
      return;
    }

    this.loading.set(true);

    this.api.getOperations()
      .pipe(
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: response => {
          this.operations.set(response.items);
        },
        error: error => {
          this.handleError(error);
        },
      });
  }

  /**
   * Determines whether administrative reconciliation is supported.
   */
  canReconcile(operation: ServiceDeskSyncOperationItem): boolean {
    return (
      operation.status === 'RequiresReconciliation' &&
      operation.processingOperationId !== null &&
      (
        operation.operationType === 'RequestCreation' ||
        operation.operationType === 'RequestClosure'
      )
    );
  }

  /**
   * Opens reconciliation for a supported operation.
   */
  openReconciliation(operation: ServiceDeskSyncOperationItem): void {
    if (operation.status !== 'RequiresReconciliation') {
      return;
    }

    if (operation.operationType === 'NoteCreation') {
      this.notify('A megjegyzés helyreállítása jelenleg nem érhető el.');
      return;
    }

    if (!this.canReconcile(operation)) {
      this.notify('A feldolgozási művelet azonosítója hiányzik.');
      return;
    }

    this.selectedOperation.set(operation);
    this.reconciliationOutcome = ServiceDeskReconciliationOutcome.Completed;
    this.verifiedRequestId = '';
    this.verificationComment = '';
  }

  /**
   * Closes the reconciliation panel.
   */
  closeReconciliation(): void {
    if (this.reconciling()) {
      return;
    }

    this.selectedOperation.set(null);
  }

  /**
   * Submits the verified reconciliation result.
   */
  submitReconciliation(): void {
    const operation = this.selectedOperation();

    if (!operation || !operation.processingOperationId) {
      return;
    }

    // Note reconciliation is not supported yet.
    if (operation.operationType === 'NoteCreation') {
      this.notify('A megjegyzés helyreállítása jelenleg nem érhető el.');
      return;
    }

    const comment = this.verificationComment.trim();

    if (!comment) {
      this.notify('Az ellenőrzési megjegyzés kötelező.');
      return;
    }

    if (comment.length > 1000) {
      this.notify('Az ellenőrzési megjegyzés legfeljebb 1000 karakter lehet.');
      return;
    }

    const isRequestCreation = operation.operationType === 'RequestCreation';

    const isCompleted = this.reconciliationOutcome === ServiceDeskReconciliationOutcome.Completed;

    const requestId = this.verifiedRequestId.trim();

    if (isRequestCreation && isCompleted && !requestId) {
      this.notify('A ServiceDesk hibajegyazonosító kötelező.');
      return;
    }

    if (requestId.length > 100) {
      this.notify('A ServiceDesk hibajegyazonosító legfeljebb 100 karakter lehet.');
      return;
    }

    const request: ResolveServiceDeskReconciliationRequest = {
      operation: isRequestCreation
        ? ServiceDeskReconciliationOperation.RequestCreation
        : ServiceDeskReconciliationOperation.RequestClosure,

      outboxId: operation.outboxId,
      processingOperationId: operation.processingOperationId,
      outcome: this.reconciliationOutcome,
      serviceDeskRequestId: isRequestCreation && isCompleted ? requestId : null,
      verificationComment: comment,
    };

    this.reconciling.set(true);

    this.api.resolveReconciliation(request)
      .pipe(
        finalize(() => this.reconciling.set(false))
      )
      .subscribe({
        next: () => {
          this.notify('A helyreállítás sikeresen megtörtént.');
          this.selectedOperation.set(null);
          this.loadOperations();
        },
        error: error => {
          this.handleError(error);
        },
      });
  }

  /**
   * Returns the Hungarian operation label.
   */
  getOperationLabel(type: ServiceDeskSyncOperationType): string {
    switch (type) {
      case 'RequestCreation':
        return 'Hibajegy létrehozása';

      case 'NoteCreation':
        return 'Megjegyzés létrehozása';

      case 'RequestClosure':
        return 'Hibajegy lezárása';
    }
  }

  /**
   * Returns the Hungarian status label.
   */
  getStatusLabel(status: ServiceDeskSyncOperationStatus): string {
    switch (status) {
      case 'Pending':
        return 'Várakozik';

      case 'Processing':
        return 'Feldolgozás alatt';

      case 'Completed':
        return 'Sikeres';

      case 'Failed':
        return 'Sikertelen';

      case 'RequiresReconciliation':
        return 'Helyreállítást igényel';
    }
  }

  /**
   * Handles API errors.
   */
  private handleError(error: unknown): void {
    if (error instanceof HttpErrorResponse) {
      const detail = error.error?.detail;

      this.notify(typeof detail === 'string' ? detail : 'A ServiceDesk művelet sikertelen.');

      return;
    }

    this.notify('Váratlan hiba történt.');
  }

  /**
   * Displays a snackbar notification.
   */
  private notify(message: string): void {
    this.snackBar.open(
      message,
      'Bezárás',
      {
        duration: 5000,
        horizontalPosition: 'right',
        verticalPosition: 'bottom',
      }
    );
  }
}