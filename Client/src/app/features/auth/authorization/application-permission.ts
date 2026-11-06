export const APPLICATION_PERMISSIONS = {
  dashboardReadAll: 'Dashboard.Read.All',
  dashboardReadOwnStore: 'Dashboard.Read.OwnStore',

  assetsReadAll: 'Assets.Read.All',
  assetsReadOwnStore: 'Assets.Read.OwnStore',

  employeesReadAll: 'Employees.Read.All',
  employeesReadOwnStore: 'Employees.Read.OwnStore',

  assetsManage: 'Assets.Manage',
  storesManage: 'Stores.Manage',
  usersManage: 'Users.Manage',

  serviceCasesReadAll: 'ServiceCases.Read.All',
  serviceCasesReadOwnStore: 'ServiceCases.Read.OwnStore',
  serviceCasesManageAll: 'ServiceCases.Manage.All',
  serviceCasesManageOwnStore: 'ServiceCases.Manage.OwnStore',
  serviceCasesClose: 'ServiceCases.Close',

  packagesReadAll: 'Packages.Read.All',
  packagesReadOwnStore: 'Packages.Read.OwnStore',
  packagesManageOwnStore: 'Packages.Manage.OwnStore',
  packagesManageAll: 'Packages.Manage.All'
} as const;

export type ApplicationPermission =
  (typeof APPLICATION_PERMISSIONS)[keyof typeof APPLICATION_PERMISSIONS];