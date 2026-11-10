import { Routes } from '@angular/router';

import { permissionGuard } from '../auth/guards/permission.guard';
import { APPLICATION_PERMISSIONS } from '../auth/authorization/application-permission';

export const settingsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./settings-layout/settings-layout').then(component => component.SettingsLayout),
    children: [
      {
        path: '',
        pathMatch: 'full',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            APPLICATION_PERMISSIONS.serviceCasesManageAll,
          ],
        },
        loadComponent: () => import('./settings-dashboard/settings-dashboard')
            .then(component => component.SettingsDashboard),
      },
      {
        path: 'service-desk',
        canActivate: [permissionGuard],
        data: {
            permissions: [
            APPLICATION_PERMISSIONS.serviceCasesManageAll,
            ],
        },
        loadComponent: () => import('../service-desk/service-desk-dashboard/service-desk-dashboard').then(component => component.ServiceDeskDashboard),
        },
    ],
  },
];