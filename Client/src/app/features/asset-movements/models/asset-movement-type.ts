export const ASSET_MOVEMENT_TYPE = [
  "Checkout",
  "Return"
] as const;

export type AssetMovementType = typeof ASSET_MOVEMENT_TYPE[number];
export const ASSET_MOVEMENT_TYPE_LABELS: Readonly<Record<AssetMovementType, string>> =
{
  Checkout: "Felvétel",
  Return: "Leadás"
}
