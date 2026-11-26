export interface ApplicationUserListItem {
  readonly id: string;
  readonly displayName: string;
  readonly email: string | null;
  readonly isActive: boolean;
}