export const STORE_DEPARTMENTS = [
  "Unknown",
  "StoreManagement",
  "StoreDeputyManagement",
  "StoreAdministration",
  "Warehouse",
  "ElectroInfo",
  "FurnitureInfo",
  "ColdHallInfo",
  "GardenInfo",
  "TilesInfo",
  "WoodInfo",
  "ToolsInfo",
  "WebshopAdministration"

] as const;

export type StoreDepartment = typeof STORE_DEPARTMENTS[number];

export const STORE_DEPARTMENTS_LABELS: Readonly<Record<StoreDepartment, string>> =
{
  Unknown: "Ismeretlen",
  StoreManagement: "Áruházvezetés",
  StoreDeputyManagement: "Helyettes áruházvezetés",
  StoreAdministration: "Áruházi adminisztráció",
  Warehouse: "Áruátvétel",
  ElectroInfo: "Elektro info",
  FurnitureInfo: "Bútor info",
  ColdHallInfo: "Hidegcsarnok info",
  GardenInfo: "Kert info",
  TilesInfo: "Csempe info",
  WoodInfo: "Fa info",
  ToolsInfo: "Szerszám info",
  WebshopAdministration: "Webshop adminisztráció"
}
