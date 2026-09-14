import { Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';
import { AppLayoutComponent } from './core/layout/app-layout';
import { permissionGuard } from './features/auth/guards/permission.guard';
import { APPLICATION_PERMISSIONS } from './features/auth/authorization/application-permission';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then(component => component.Login),
  },
  {
    path: '',
    component: AppLayoutComponent,
    canActivate: [MsalGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.dashboardReadAll,
            APPLICATION_PERMISSIONS.dashboardReadOwnStore,
          ]
        },
        loadComponent: () => import('./features/dashboard/dashboard').then(component => component.Dashboard),
      },
      {
        path: 'assets/new',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.assetsManage
          ]
        },
        loadComponent: () => import('./features/assets/asset-create/asset-create').then(component => component.AssetCreate),
      },
      {
        path: 'assets/:id/edit',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.assetsManage
          ],
        },
        loadComponent: () => import('./features/assets/asset-update/asset-update').then(component => component.AssetUpdate),
      },
      {
        path: 'assets/:id',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.assetsReadAll,
            APPLICATION_PERMISSIONS.assetsReadOwnStore
          ],
        },
        loadComponent: () => import('./features/assets/asset-details/asset-details').then(component => component.AssetDetails),
      },
      {
        path: 'assets',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.assetsReadAll,
            APPLICATION_PERMISSIONS.assetsReadOwnStore
          ],
        },
        loadComponent: () => import('./features/assets/asset-list/asset-list').then(component => component.AssetList),
      },
      {
        path: 'stores/new',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.storesManage
          ],
        },
        loadComponent: () =>
          import('./features/stores/store-create/store-create').then(component => component.StoreCreate),
      },
      {
        path: 'stores',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.storesManage
          ],
        },
        loadComponent: () =>
          import('./features/stores/store-list/store-list').then(component => component.StoreList),
      },
      {
        path: 'employees',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.employeesReadAll,
          ],
        },
        loadComponent: () =>import('./features/employees/employee-list/employee-list').then(component => component.EmployeeList),
      },
      {
        path: 'vendors/new',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.assetsManage
          ],
        },
        loadComponent: () => import('./features/vendors/vendor-create/vendor-create').then(component => component.VendorCreate),
      },
      {
        path: 'vendors',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.assetsManage
          ],
        },
        loadComponent: () => import('./features/vendors/vendor-list/vendor-list').then(component => component.VendorList),
      },
      
      {
        path: 'forbidden',
        loadComponent: () =>
          import(
            './features/auth/forbidden/forbidden'
          ).then(component => component.Forbidden),
      },
    ],
  },
  {
        path: 'asset-movement',
        loadComponent: () => import('./features/asset-movements/asset-movement-create/asset-movement-create').then(component => component.AssetMovementCreate),
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];