export enum ServiceIssueType {
  Unknown = 0,
  DoesNotTurnOn = 1,
  PhysicalDamage = 2,
  ChargingIssue = 3,
  DisplayIssue = 4,
  NetworkIssue = 5,
  SoftwareIssue = 6,
  ContinuousRestart = 7,
  FaultyButton = 8,
  Other = 99,
}

export type ServiceIssueTypeValue =
  | ServiceIssueType
  | keyof typeof ServiceIssueType;

  export const SERVICE_ISSUE_TYPE_LABELS:
  Readonly<Record<ServiceIssueType, string>> = {
    [ServiceIssueType.Unknown]: 'Ismeretlen',
    [ServiceIssueType.DoesNotTurnOn]: 'Nem kapcsol be',
    [ServiceIssueType.PhysicalDamage]: 'Fizikai sérülés',
    [ServiceIssueType.ChargingIssue]: 'Töltési probléma',
    [ServiceIssueType.DisplayIssue]: 'Kijelzőhiba',
    [ServiceIssueType.NetworkIssue]: 'Hálózati probléma',
    [ServiceIssueType.SoftwareIssue]: 'Szoftverhiba',
    [ServiceIssueType.ContinuousRestart]: 'Folyamatos újraindulás',
    [ServiceIssueType.FaultyButton]: 'Hibás gomb',
    [ServiceIssueType.Other]: 'Egyéb',
  };

export function getServiceIssueTypeLabel(value: ServiceIssueTypeValue | null | undefined): string {
  if (value === null || value === undefined) {
    return 'Nincs megadva';
  }

  const issueType = typeof value === 'number' ? value : ServiceIssueType[value];

  return SERVICE_ISSUE_TYPE_LABELS[issueType] ?? 'Ismeretlen hibatípus';
}