export interface StoreDashboardResponse {
  readonly storeId: string;
  readonly storeNumber: string;
  readonly storeName: string;
  readonly totalPDTs: number;
  readonly usedPDTs: number;
  readonly servicePDTs: number;
  readonly availablePDTs: number;
}
