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
      APPLICATION_PERMISSIONS.usersManage,
      APPLICATION_PERMISSIONS.serviceCasesManageAll,
      APPLICATION_PERMISSIONS.serviceCasesReadAll,
      APPLICATION_PERMISSIONS.serviceCasesClose,
      APPLICATION_PERMISSIONS.packagesManageAll,
      APPLICATION_PERMISSIONS.packagesReadAll
    ],

    CentralUser: [
      APPLICATION_PERMISSIONS.dashboardReadAll,
      APPLICATION_PERMISSIONS.assetsReadAll,
      APPLICATION_PERMISSIONS.serviceCasesReadAll,
      APPLICATION_PERMISSIONS.packagesReadAll
    ],

    StoreManagement: [
      APPLICATION_PERMISSIONS.dashboardReadOwnStore,
      APPLICATION_PERMISSIONS.assetsReadOwnStore,
      APPLICATION_PERMISSIONS.serviceCasesManageOwnStore,
      APPLICATION_PERMISSIONS.serviceCasesReadOwnStore,
      APPLICATION_PERMISSIONS.packagesReadOwnStore,
      APPLICATION_PERMISSIONS.packagesManageOwnStore
    ],

    StoreAdministrator: [
      APPLICATION_PERMISSIONS.dashboardReadOwnStore,
      APPLICATION_PERMISSIONS.assetsReadOwnStore,
      APPLICATION_PERMISSIONS.serviceCasesReadOwnStore,
      APPLICATION_PERMISSIONS.serviceCasesManageOwnStore,
      APPLICATION_PERMISSIONS.packagesReadOwnStore,
      APPLICATION_PERMISSIONS.packagesManageOwnStore
    ],
    HotlineAgent: [
      APPLICATION_PERMISSIONS.dashboardReadAll,
      APPLICATION_PERMISSIONS.assetsReadAll,
      APPLICATION_PERMISSIONS.assetsManage,
      APPLICATION_PERMISSIONS.serviceCasesManageAll,
      APPLICATION_PERMISSIONS.serviceCasesReadAll,
      APPLICATION_PERMISSIONS.packagesManageAll,
      APPLICATION_PERMISSIONS.packagesReadAll
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