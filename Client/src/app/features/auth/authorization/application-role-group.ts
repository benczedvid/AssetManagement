import { ApplicationRole } from
  '../data-access/models/application-role';

export const ALL_APPLICATION_ROLES = [
  'ApplicationAdministrator',
  'CentralUser',
  'StoreManagement',
  'StoreAdministrator',
] as const satisfies readonly ApplicationRole[];

export const DASHBOARD_VIEW_ROLES = [
  'ApplicationAdministrator',
  'CentralUser',
  'StoreManagement',
  'StoreAdministrator',
] as const satisfies readonly ApplicationRole[];

export const ASSET_VIEW_ROLES = [
  'ApplicationAdministrator',
  'CentralUser',
  'StoreManagement',
  'StoreAdministrator',
] as const satisfies readonly ApplicationRole[];

export const ASSET_MANAGEMENT_ROLES = [
  'ApplicationAdministrator',
] as const satisfies readonly ApplicationRole[];

export const STORE_VIEW_ROLES = [
  'ApplicationAdministrator',
  'CentralUser',
] as const satisfies readonly ApplicationRole[];

export const STORE_MANAGEMENT_ROLES = [
  'ApplicationAdministrator',
] as const satisfies readonly ApplicationRole[];

export const EMPLOYEE_VIEW_ROLES = [
  'ApplicationAdministrator',
  'StoreManagement',
  'StoreAdministrator',
] as const satisfies readonly ApplicationRole[];

export const EMPLOYEE_MANAGEMENT_ROLES = [
  'ApplicationAdministrator',
] as const satisfies readonly ApplicationRole[];

export const USER_MANAGEMENT_ROLES = [
  'ApplicationAdministrator',
] as const satisfies readonly ApplicationRole[];