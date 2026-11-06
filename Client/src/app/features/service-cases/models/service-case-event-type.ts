export enum ServiceCaseEventType {
  Unknown = 0,
  Created = 1,
  DispatchedToHeadquarters = 2,
  InspectionStarted = 3,
  RepairCompletedInHeadquarters = 4,
  ExternalServiceRequired = 5,
  DispatchedToExternalService = 6,
  ReceivedFromExternalService = 7,
  ReturnedFromExternalService = 8,
  ReceivedInStore = 9,
  Resolved = 10,
  Closed = 11,
  Cancelled = 12,
  ReceivedAtHeadquarters = 13,
  HandedOverToExternalService = 14,
  ReceivedByExternalService = 15,
  DispatchedToOriginStore = 16,
  AssetDisposed = 17
}

export type ServiceCaseEventTypeValue =
  | ServiceCaseEventType
  | keyof typeof ServiceCaseEventType;

export const SERVICE_CASE_EVENT_TYPE_LABELS: Readonly<Record<ServiceCaseEventType, string>> = {
    [ServiceCaseEventType.Unknown]: 'Ismeretlen esemény',
    [ServiceCaseEventType.Created]: 'Ügy létrehozva',
    [ServiceCaseEventType.DispatchedToHeadquarters]: 'Elküldve a központba',
    [ServiceCaseEventType.InspectionStarted]: 'Bevizsgálás megkezdve',
    [ServiceCaseEventType.RepairCompletedInHeadquarters]: 'Központi javítás befejezve',
    [ServiceCaseEventType.ExternalServiceRequired]: 'Külső szerviz kijelölve',
    [ServiceCaseEventType.DispatchedToExternalService]: 'Elküldve külső szervizbe',
    [ServiceCaseEventType.ReceivedFromExternalService]: 'Átvétel külső szervizből rögzítve',
    [ServiceCaseEventType.ReturnedFromExternalService]: 'Visszaérkezés külső szervizből rögzítve',
    [ServiceCaseEventType.ReceivedInStore]: 'Áruházi visszavétel rögzítve',
    [ServiceCaseEventType.Resolved]: 'Ügy megoldva',
    [ServiceCaseEventType.Closed]: 'Ügy véglegesen lezárva',
    [ServiceCaseEventType.Cancelled]: 'Ügy visszavonva',
    [ServiceCaseEventType.ReceivedAtHeadquarters]: 'Központi átvétel rögzítve',
    [ServiceCaseEventType.HandedOverToExternalService]: 'Közvetlenül átadva a szerviznek',
    [ServiceCaseEventType.ReceivedByExternalService]: 'Szerviz általi átvétel megerősítve',
    [ServiceCaseEventType.DispatchedToOriginStore]: 'Visszaküldve az áruházba',
    [ServiceCaseEventType.AssetDisposed]: "Selejtezve"
  };

export function getServiceCaseEventTypeLabel(value: ServiceCaseEventTypeValue | null | undefined): string {
  if (value === null || value === undefined) {
    return 'Nincs megadva';
  }

  const eventType = typeof value === 'number' ? value : ServiceCaseEventType[value];

  return SERVICE_CASE_EVENT_TYPE_LABELS[eventType] ?? 'Ismeretlen esemény';
}