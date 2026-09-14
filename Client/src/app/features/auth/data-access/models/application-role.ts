export const APPLICATION_ROLES = [
  'Unknown',
  'ApplicationAdministrator',
  'CentralUser',
  'StoreManagement',
  'StoreAdministrator'
] as const;

export type ApplicationRole =
  (typeof APPLICATION_ROLES)[number];

export const APPLICATION_ROLE_LABELS:
  Readonly<Record<ApplicationRole, string>> = {
    Unknown: 'Ismeretlen',
    ApplicationAdministrator:'Alkalmazásadminisztrátor',
    CentralUser: 'Központi felhasználó',
    StoreManagement: 'Áruházvezetés',
    StoreAdministrator: 'Áruházi adminisztrátor'
  };
