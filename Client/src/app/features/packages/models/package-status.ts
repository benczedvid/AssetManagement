export const PACKAGE_STATUSES = [
  'Unknown',
  'Created',
  'InTransit',
  'Received',
  'Cancelled',
] as const;

export type PackageStatus =
  typeof PACKAGE_STATUSES[number];

export const PACKAGE_STATUS_LABELS: Readonly<
  Record<PackageStatus, string>
> = {
  Unknown: 'Ismeretlen',
  Created: 'Előkészített',
  InTransit: 'Úton',
  Received: 'Megérkezett',
  Cancelled: 'Megszakítva',
};

export function getPackageStatusLabel(status: PackageStatus): string {
  return PACKAGE_STATUS_LABELS[status];
}

export function isActivePackageStatus(status: PackageStatus): boolean {
  return status === 'Created' || status === 'InTransit';
}