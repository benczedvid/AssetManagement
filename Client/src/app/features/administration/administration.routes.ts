import { Routes } from '@angular/router';

import { permissionGuard } from '../auth/guards/permission.guard';
import { APPLICATION_PERMISSIONS } from '../auth/authorization/application-permission';

export const administrationRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./administration-layout/administration-layout')
        .then(component => component.AdministrationLayout),
    children: [
      {
        path: '',
        pathMatch: 'full',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.serviceCasesReadAll,
            APPLICATION_PERMISSIONS.serviceCasesReadOwnStore,
          ],
        },
        loadComponent: () =>
          import('./administration-dashboard/administration-dashboard')
            .then(component => component.AdministrationDashboard),
      },
      {
        path: 'service',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.serviceCasesReadAll,
            APPLICATION_PERMISSIONS.serviceCasesReadOwnStore,
          ],
        },
        loadComponent: () =>
          import('../service-cases/service-cases-list/service-cases-list')
            .then(component => component.ServiceCasesList),
      },

      /*
       * A Packages route jogosultságát akkor kötjük be a
       * permissionGuarddal, amikor látjuk a tényleges
       * package permission konstansokat.
       *
       * Nem használunk feltételezett packagesReadAll vagy
       * packagesManage konstansokat.
       */
      {
        path: 'packages',
        loadComponent: () => import('../packages/package-list/package-list').then(component => component.PackageList),
      },
    ],
  },
];