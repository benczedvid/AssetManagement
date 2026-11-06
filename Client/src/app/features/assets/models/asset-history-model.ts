export interface AssetHistoryModel {
  id: string;
  assetId: string;
  action: string;
  propertyName: string;
  oldValue: string | null;
  newValue: string | null;
  changedByUserId: string;
  changedByUserName: string;
  changedAtUtc: string;
}

export const ASSET_HISTORY_PROPERTY_LABELS: Readonly<Record<string, string>> = {
  AssetName: 'Eszköz neve',
  AssetType: 'Eszköz típusa',
  AssetStatus: 'Eszköz állapota',
  Manufacturer: 'Gyártó',
  Model: 'Modell',
  RfidTagId: 'RFID-azonosító',
  MacAddress: 'MAC-cím',
  WiFiMacAddress: 'Wi-Fi MAC-cím',
  Imei: 'IMEI',
  OperatingSystem: 'Operációs rendszer',
  OperatingSystemVersion: 'Operációs rendszer verziója',
  AssignedStoreId: 'Hozzárendelt áruház',
  AssignedUserId: 'Hozzárendelt felhasználó',
  AssignedVendorId: 'Hozzárendelt szállító',
  WarrantyEndDate: 'Garancia lejárat'
};