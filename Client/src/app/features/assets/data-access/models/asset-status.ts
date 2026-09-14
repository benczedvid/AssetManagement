export const ASSET_STATUSES = [
  "Unknown",
  "In_HQ",
  "Used_In_HQ",
  "In_Store",
  "Used_In_Store",
  "In_Service",
  "Route_To_Store",
  "Route_To_HQ",
  "Route_To_Service",
  "Disposed"
] as const;

export type AssetStatus = typeof ASSET_STATUSES[number];

export const ASSET_STATUS_LABELS: Readonly<Record<AssetStatus, string>> =
{
  Unknown: "Ismeretlen",
  In_HQ: "Központban",
  Used_In_HQ: "Központi használatban",
  In_Store: "Áruházban",
  Used_In_Store: "Áruházi használatban",
  In_Service: "Szervizben",
  Route_To_Store: "Úton az áruházba",
  Route_To_HQ: "Úton a központba",
  Route_To_Service: "Úton a szervizbe",
  Disposed: "Selejtezve"
};
