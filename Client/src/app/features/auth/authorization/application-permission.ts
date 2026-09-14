export const APPLICATION_PERMISSIONS = {
  dashboardReadAll: 'Dashboard.Read.All',
  dashboardReadOwnStore: 'Dashboard.Read.OwnStore',

  assetsReadAll: 'Assets.Read.All',
  assetsReadOwnStore: 'Assets.Read.OwnStore',

  employeesReadAll: 'Employees.Read.All',
  employeesReadOwnStore: 'Employees.Read.OwnStore',

  assetsManage: 'Assets.Manage',
  storesManage: 'Stores.Manage',
  usersManage: 'Users.Manage'
} as const;

export type ApplicationPermission =
  (typeof APPLICATION_PERMISSIONS)[keyof typeof APPLICATION_PERMISSIONS];