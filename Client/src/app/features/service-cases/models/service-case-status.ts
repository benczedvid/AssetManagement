export enum ServiceCaseStatus {
  Unknown = 0,
  AwaitingCentralReceipt = 1,
  UnderCentralInspection = 2,
  AwaitingExternalServiceDispatch = 3,
  SentToExternalService = 4,
  InExternalService = 5,
  AwaitingReturn = 6,
  ReturningToStore = 7,
  Resolved = 8,
  Closed = 9,
  Cancelled = 10,
}

export type ServiceCaseStatusValue =
  | ServiceCaseStatus
  | keyof typeof ServiceCaseStatus;

export const SERVICE_CASE_STATUS_LABELS:
  Readonly<Record<ServiceCaseStatus, string>> = {
    [ServiceCaseStatus.Unknown]: 'Ismeretlen',
    [ServiceCaseStatus.AwaitingCentralReceipt]: 'Központi átvételre vár',
    [ServiceCaseStatus.UnderCentralInspection]: 'Központi bevizsgálás alatt',
    [ServiceCaseStatus.AwaitingExternalServiceDispatch]: 'Külső szervizbe küldésre vár',
    [ServiceCaseStatus.SentToExternalService]: 'Külső szervizbe elküldve',
    [ServiceCaseStatus.InExternalService]: 'Külső szervizben',
    [ServiceCaseStatus.AwaitingReturn]: 'Visszaküldésre vagy megoldásra vár',
    [ServiceCaseStatus.ReturningToStore]: 'Visszaszállítás alatt az áruházba',
    [ServiceCaseStatus.Resolved]: 'Megoldva',
    [ServiceCaseStatus.Closed]: 'Lezárva',
    [ServiceCaseStatus.Cancelled]: 'Visszavonva',
  };

  export function getServiceCaseStatusLabel(value: ServiceCaseStatusValue | null | undefined): string {
  if (value === null || value === undefined) {
    return 'Nincs megadva';
  }

  const status = typeof value === 'number' ? value : ServiceCaseStatus[value];

  return SERVICE_CASE_STATUS_LABELS[status] ?? 'Ismeretlen állapot';
}