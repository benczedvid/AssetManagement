export const SERVICE_CASE_ACTIONS = {
  receiveAtHeadquarters: 'ReceiveAtHeadquarters',
  requireExternalService: 'RequireExternalService',
  dispatchToExternalService: 'DispatchToExternalService',
  handOverToExternalService: 'HandOverToExternalService',
  confirmReceivedByExternalService: 'ConfirmReceivedByExternalService',
  returnFromExternalService: 'ReturnFromExternalService',
  completeRepairAtHeadquarters: 'CompleteRepairAtHeadquarters',
  dispatchToOriginStore: 'DispatchToOriginStore',
  receiveAtOriginStore: 'ReceiveAtOriginStore',
  resolveAfterExternalServiceReturn: 'ResolveAfterExternalServiceReturn',
  disposeAsset: 'DisposeAsset',
  close: 'Close',
} as const;

export type ServiceCaseAction = (typeof SERVICE_CASE_ACTIONS)[keyof typeof SERVICE_CASE_ACTIONS];

export const SERVICE_CASE_ACTION_LABELS: Readonly<Record<ServiceCaseAction, string>> = {
    ReceiveAtHeadquarters: 'Központi átvétel',
    RequireExternalService: 'Külső szerviz kijelölése',
    DispatchToExternalService: 'Elküldés külső szervizbe',
    HandOverToExternalService: 'Közvetlen átadás a szerviznek',
    ConfirmReceivedByExternalService: 'Szerviz átvételének megerősítése',
    ReturnFromExternalService: 'Visszaérkezés rögzítése a szervizből',
    CompleteRepairAtHeadquarters: 'Központi javítás befejezése',
    DispatchToOriginStore: 'Visszaküldés az eredeti áruházba',
    ReceiveAtOriginStore: 'Áruházi visszavétel',
    ResolveAfterExternalServiceReturn: 'Megoldottra állítás',
    DisposeAsset: 'Eszköz selejtezése',
    Close: 'Végleges lezárás',
  };

export function getServiceCaseActionLabel(action: ServiceCaseAction | null | undefined): string {
  if (action === null || action === undefined) {
    return 'Nincs megadva';
  }

  return SERVICE_CASE_ACTION_LABELS[action] ?? 'Ismeretlen művelet';
}