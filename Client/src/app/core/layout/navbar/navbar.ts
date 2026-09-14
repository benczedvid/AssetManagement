import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { CurrentUserApi } from '../../../features/auth/current-user-api';
import { APPLICATION_ROLE_LABELS, ApplicationRole } from '../../../features/auth/data-access/models/application-role';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './navbar.html'
})
export class NavbarComponent {
  private readonly authService = inject(MsalService);
  readonly currentUserApi = inject(CurrentUserApi);
  readonly isLogoutInProgress = signal(false);
  readonly canViewDashboard = computed(() => this.currentUserApi.canViewDashboard());
  readonly canViewAssets = computed(() => this.currentUserApi.canViewAssets());
  readonly canViewEmployees = computed(() => this.currentUserApi.canViewEmployees());
  readonly canManageAssets = computed(() => this.currentUserApi.canManageAssets());
  readonly canManageStores = computed(() => this.currentUserApi.canManageStores());
  readonly canManageUsers = computed(() => this.currentUserApi.canManageUsers());
  readonly canViewVendors = computed(() => this.currentUserApi.canManageAssets());

  logout(): void {
    if (this.isLogoutInProgress()) {
      return;
    }

    this.isLogoutInProgress.set(true);

    const activeAccount = this.authService.instance.getActiveAccount();

    this.currentUserApi.clear();

    this.authService.logoutRedirect({
      account: activeAccount ?? undefined
    }).subscribe({
      error: error => {
        console.error(
          'The user could not be signed out.',
          error
        );

        this.isLogoutInProgress.set(false);
      }
    });
  }

  getRoleLabel(
    role: ApplicationRole
  ): string {
    return APPLICATION_ROLE_LABELS[role];
  }
}