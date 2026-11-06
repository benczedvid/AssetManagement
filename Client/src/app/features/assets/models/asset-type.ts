export const ASSET_TYPE = [
  "Unknown",
  "DesktopComputer",
  "Notebook",
  "ThinClient",
  "Monitor",
  "Printer",
  "PDT"
] as const;

export type AssetType = typeof ASSET_TYPE[number];

export const ASSET_TYPE_LABELS: Readonly<Record<AssetType, string>> =
{
  Unknown: "Ismeretlen",
  DesktopComputer: "Asztali számítógép",
  Notebook: "Notebook",
  ThinClient: "Vékonykliens",
  Monitor: "Monitor",
  Printer: "Nyomtató",
  PDT: "PDT"
};
