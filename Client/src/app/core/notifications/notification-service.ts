import { inject, Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string, duration= 5000): void {
    this.open(message, ['asset-management-snackbar', 'asset-management-success'], duration);
  }
  error(message: string, duration= 5000): void {
    this.open(message, ['asset-management-snackbar', 'asset-management-error'], duration);
  }
  warning(message: string, duration= 5000): void {
    this.open(message, ['asset-management-snackbar', 'asset-management-warning'], duration);
  }
  info(message: string, duration= 4000): void {
    this.open(message, ['asset-management-snackbar', 'asset-management-info'], duration);
  }
  dismiss(): void {
    this.snackBar.dismiss();
  }
  private open(message: string, panelClass: string[], duration: number) {
      const config: MatSnackBarConfig = {
        duration,
        horizontalPosition: 'right',
        verticalPosition: 'bottom',
        politeness: 'polite',
        panelClass,
      };
    this.snackBar.open(message, 'Bezárás', config);
  }
}
