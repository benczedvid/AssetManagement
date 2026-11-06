import { ApplicationRole } from './application-role';

export interface CurrentUserModel {
  readonly id: string;
  readonly entraObjectId: string;
  readonly entraTenantId: string;
  readonly employeeNumber: string | null;
  readonly firstName: string | null;
  readonly lastName: string | null;
  readonly displayName: string;
  readonly role: ApplicationRole;
  readonly storeId: string | null;
  readonly canAccessAllStores: boolean;
  readonly isActive: boolean;
}