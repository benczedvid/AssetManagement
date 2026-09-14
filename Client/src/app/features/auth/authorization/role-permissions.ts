import { APPLICATION_PERMISSIONS, ApplicationPermission } from './application-permission';

import { ApplicationRole} from '../data-access/models/application-role';

const ROLE_PERMISSIONS:
  Readonly<Record<ApplicationRole, readonly ApplicationPermission[]>> = {
    Unknown: [],

    ApplicationAdministrator: [
      APPLICATION_PERMISSIONS.dashboardReadAll,
      APPLICATION_PERMISSIONS.assetsReadAll,
      APPLICATION_PERMISSIONS.employeesReadAll,
      APPLICATION_PERMISSIONS.assetsManage,
      APPLICATION_PERMISSIONS.storesManage,
      APPLICATION_PERMISSIONS.usersManage
    ],

    CentralUser: [
      APPLICATION_PERMISSIONS.dashboardReadAll,
      APPLICATION_PERMISSIONS.assetsReadAll
    ],

    StoreManagement: [
      APPLICATION_PERMISSIONS.dashboardReadOwnStore,
      APPLICATION_PERMISSIONS.assetsReadOwnStore,
      APPLICATION_PERMISSIONS.employeesReadOwnStore
    ],

    StoreAdministrator: [
      APPLICATION_PERMISSIONS.dashboardReadOwnStore,
      APPLICATION_PERMISSIONS.assetsReadOwnStore,
    ]
  };

export function getRolePermissions(role: ApplicationRole): readonly ApplicationPermission[] {
  return ROLE_PERMISSIONS[role];
}

export function hasPermission(role: ApplicationRole, permission: ApplicationPermission): boolean {
  return ROLE_PERMISSIONS[role].includes(permission);
}

export function hasAnyPermission(role: ApplicationRole, permissions: readonly ApplicationPermission[]): boolean {
  return permissions.some(permission => hasPermission(role, permission)
  );
}