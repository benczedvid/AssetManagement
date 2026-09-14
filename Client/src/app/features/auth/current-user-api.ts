import { HttpClient } from '@angular/common/http';

import {
  computed,
  inject,
  Injectable,
  signal,
} from '@angular/core';

import {
  finalize,
  Observable,
  of,
  shareReplay,
  tap,
} from 'rxjs';

import {
  environment
} from '../../../environments/environment';

import {
  ApplicationPermission
} from '../auth/authorization/application-permission';

import {
  ApplicationRole
} from './data-access/models/application-role';

import {
  CurrentUserModel
} from './data-access/models/current-user-model';

import {
  hasAnyPermission as roleHasAnyPermission,
  hasPermission as roleHasPermission,
} from '../auth/authorization/role-permissions';

@Injectable({
  providedIn: 'root',
})
export class CurrentUserApi {
  private readonly http = inject(HttpClient);

  private readonly currentUserSignal =
    signal<CurrentUserModel | null>(null);

  private readonly isLoadingSignal =
    signal(false);

  private loadingRequest:
    Observable<CurrentUserModel> | null = null;

  readonly currentUser =
    this.currentUserSignal.asReadonly();

  readonly isLoading =
    this.isLoadingSignal.asReadonly();

  readonly isInitialized = computed(
    () => this.currentUserSignal() !== null
  );

  readonly isActive = computed(
    () =>
      this.currentUserSignal()?.isActive === true
  );

  readonly role = computed(
    () =>
      this.currentUserSignal()?.role ?? null
  );

  readonly employeeNumber = computed(
    () =>
      this.currentUserSignal()?.employeeNumber ?? null
  );

  readonly storeId = computed(
    () =>
      this.currentUserSignal()?.storeId ?? null
  );

  readonly canAccessAllStores = computed(
    () =>
      this.currentUserSignal()
        ?.canAccessAllStores === true
  );

  readonly isApplicationAdministrator = computed(
    () =>
      this.hasRole(
        'ApplicationAdministrator'
      )
  );

  readonly isCentralUser = computed(
    () =>
      this.hasRole(
        'CentralUser'
      )
  );

  readonly isStoreManagement = computed(
    () =>
      this.hasRole(
        'StoreManagement'
      )
  );

  readonly isStoreAdministrator = computed(
    () =>
      this.hasRole(
        'StoreAdministrator'
      )
  );

  readonly hasGlobalStoreScope = computed(
    () => {
      const currentUser =
        this.currentUserSignal();

      return (
        currentUser !== null &&
        currentUser.isActive &&
        currentUser.canAccessAllStores
      );
    }
  );

  readonly hasOwnStoreScope = computed(
    () => {
      const currentUser =
        this.currentUserSignal();

      return (
        currentUser !== null &&
        currentUser.isActive &&
        !currentUser.canAccessAllStores &&
        currentUser.storeId !== null
      );
    }
  );

  getOrCreateCurrentUser():
    Observable<CurrentUserModel> {
    if (this.loadingRequest) {
      return this.loadingRequest;
    }

    const existingUser =
      this.currentUserSignal();

    if (existingUser) {
      return of(existingUser);
    }

    this.isLoadingSignal.set(true);

    const request = this.http
      .get<CurrentUserModel>(
        `${environment.api.baseUrl}/users/me`
      )
      .pipe(
        tap(user => {
          console.log(
            'Current AssetManagement user:',
            user
          );

          console.log(
            'Current application role:',
            user.role
          );

          console.log(
            'Current user is active:',
            user.isActive
          );

          console.log(
            'Current user store ID:',
            user.storeId
          );

          console.log(
            'Current user can access all stores:',
            user.canAccessAllStores
          );

          this.currentUserSignal.set(user);
        }),

        finalize(() => {
          this.isLoadingSignal.set(false);
          this.loadingRequest = null;
        }),

        shareReplay({
          bufferSize: 1,
          refCount: false,
        })
      );

    this.loadingRequest = request;

    return request;
  }

  hasRole(
    role: ApplicationRole
  ): boolean {
    const currentUser =
      this.currentUserSignal();

    return (
      currentUser !== null &&
      currentUser.isActive &&
      currentUser.role === role
    );
  }

  hasAnyRole(
    ...allowedRoles:
      readonly ApplicationRole[]
  ): boolean {
    const currentUser =
      this.currentUserSignal();

    if (!currentUser?.isActive) {
      return false;
    }

    return allowedRoles.includes(
      currentUser.role
    );
  }

  hasPermission(
    permission: ApplicationPermission
  ): boolean {
    const currentUser =
      this.currentUserSignal();

    if (!currentUser?.isActive) {
      return false;
    }

    return roleHasPermission(
      currentUser.role,
      permission
    );
  }

  hasAnyPermission(
    requiredPermissions:
      readonly ApplicationPermission[]
  ): boolean {
    const currentUser =
      this.currentUserSignal();

    if (!currentUser?.isActive) {
      return false;
    }

    return roleHasAnyPermission(
      currentUser.role,
      requiredPermissions
    );
  }

  canViewDashboard(): boolean {
    return this.hasAnyPermission([
      'Dashboard.Read.All',
      'Dashboard.Read.OwnStore',
    ]);
  }

  canViewAssets(): boolean {
    return this.hasAnyPermission([
      'Assets.Read.All',
      'Assets.Read.OwnStore',
    ]);
  }

  canViewEmployees(): boolean {
    return this.hasAnyPermission([
      'Employees.Read.All',
      'Employees.Read.OwnStore',
    ]);
  }

  canManageAssets(): boolean {
    return this.hasPermission(
      'Assets.Manage'
    );
  }

  canManageStores(): boolean {
    return this.hasPermission(
      'Stores.Manage'
    );
  }

  canManageUsers(): boolean {
    return this.hasPermission(
      'Users.Manage'
    );
  }

  clear(): void {
    this.currentUserSignal.set(null);
    this.loadingRequest = null;
    this.isLoadingSignal.set(false);
  }
}