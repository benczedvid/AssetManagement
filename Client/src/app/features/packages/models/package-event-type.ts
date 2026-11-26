export const PackageEventType = {
  Unknown: 'Unknown',
  Created: 'Created',
  AssetAdded: 'AssetAdded',
  AssetRemoved: 'AssetRemoved',
  CourierAssigned: 'CourierAssigned',
  CourierChanged: 'CourierChanged',
  CourierRemoved: 'CourierRemoved',
  Dispatched: 'Dispatched',
  Received: 'Received',
  Cancelled: 'Cancelled',
} as const;

export type PackageEventTypeValue = typeof PackageEventType[keyof typeof PackageEventType];

export function getPackageEventTypeLabel(type: PackageEventTypeValue): string {
  switch (type) {
    case PackageEventType.Created:
      return 'Csomag létrehozva';

    case PackageEventType.AssetAdded:
      return 'Eszköz hozzáadva';

    case PackageEventType.AssetRemoved:
      return 'Eszköz eltávolítva';

    case PackageEventType.CourierAssigned:
      return 'Futár hozzárendelve';

    case PackageEventType.CourierChanged:
      return 'Futár módosítva';

    case PackageEventType.CourierRemoved:
      return 'Futár eltávolítva';

    case PackageEventType.Dispatched:
      return 'Csomag feladva';

    case PackageEventType.Received:
      return 'Csomag érkeztetve';

    case PackageEventType.Cancelled:
      return 'Csomag megszakítva';

    case PackageEventType.Unknown:
    default:
      return 'Ismeretlen esemény';
  }
}