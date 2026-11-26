import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

import type { AssignCourierRequest } from '../models/assign-courier-request';
import type { AssignCourierResponse } from '../models/assign-courier-response';

import type { CancelPackageRequest } from '../models/cancel-package-request';
import type { CancelPackageResponse } from '../models/cancel-package-response';

import type { CreatePackageRequest } from '../models/create-package-request';
import type { CreatePackageResponse } from '../models/create-package-response';

import type { DispatchPackageRequest } from '../models/dispatch-package-request';
import type { DispatchPackageResponse } from '../models/dispatch-package-response';

import type { GetPackagesResponse } from '../models/get-packages-response';
import type { PackageDetails } from '../models/package-details';

import type { ReceivePackageRequest } from '../models/receive-package-request';
import type { ReceivePackageResponse } from '../models/receive-package-response';

import type { AddPackageAssetRequest } from '../models/add-package-asset-request';
import type { AddPackageAssetResponse } from '../models/add-package-asset-response';
import type { RemovePackageAssetResponse } from '../models/remove-package-asset-response';

export {
  PACKAGE_STATUSES,
  PACKAGE_STATUS_LABELS,
  getPackageStatusLabel,
  isActivePackageStatus
} from '../models/package-status';

export type { PackageStatus } from '../models/package-status';

export {
  PackageEventType,
  getPackageEventTypeLabel
} from '../models/package-event-type';

export type { PackageEventTypeValue } from '../models/package-event-type';

export type { PackageListItem } from '../models/package-list-item';
export type { PackageItem } from '../models/package-item';
export type { PackageEvent } from '../models/package-event';
export type { PackageDetails } from '../models/package-details';
export type { GetPackagesResponse } from '../models/get-packages-response';

export type { CreatePackageRequest } from '../models/create-package-request';
export type { CreatePackageResponse } from '../models/create-package-response';

export type { AssignCourierRequest } from '../models/assign-courier-request';
export type { AssignCourierResponse } from '../models/assign-courier-response';

export type { DispatchPackageRequest } from '../models/dispatch-package-request';

export type {
  DispatchPackageResponse,
  DispatchPackageAssetResponse
} from '../models/dispatch-package-response';

export type { ReceivePackageRequest } from '../models/receive-package-request';

export type {
  ReceivePackageResponse,
  ReceivePackageAssetResponse
} from '../models/receive-package-response';

export type { CancelPackageRequest } from '../models/cancel-package-request';
export type { CancelPackageResponse } from '../models/cancel-package-response';

export type { AddPackageAssetRequest } from '../models/add-package-asset-request';
export type { AddPackageAssetResponse } from '../models/add-package-asset-response';
export type { RemovePackageAssetResponse } from '../models/remove-package-asset-response';

@Injectable({
  providedIn: 'root'
})
export class PackagesApi {

  private readonly http = inject(HttpClient);

  private readonly baseUrl =
    `${environment.api.baseUrl.replace(/\/+$/, '')}/packages`;

  getList(): Observable<GetPackagesResponse> {
    return this.http.get<GetPackagesResponse>(
      this.baseUrl
    );
  }

  getDetails(id: string): Observable<PackageDetails> {
    return this.http.get<PackageDetails>(
      this.getPackageUrl(id)
    );
  }

  create(
    request: CreatePackageRequest
  ): Observable<CreatePackageResponse> {
    return this.http.post<CreatePackageResponse>(
      this.baseUrl,
      request
    );
  }

  assignCourier(
    id: string,
    request: AssignCourierRequest
  ): Observable<AssignCourierResponse> {
    return this.http.put<AssignCourierResponse>(
      `${this.getPackageUrl(id)}/courier`,
      request
    );
  }

  dispatch(
    id: string,
    request: DispatchPackageRequest = {
      comment: null
    }
  ): Observable<DispatchPackageResponse> {
    return this.http.post<DispatchPackageResponse>(
      `${this.getPackageUrl(id)}/dispatch`,
      request
    );
  }

  receive(
    id: string,
    request: ReceivePackageRequest
  ): Observable<ReceivePackageResponse> {
    return this.http.post<ReceivePackageResponse>(
      `${this.getPackageUrl(id)}/receive`,
      request
    );
  }

  cancel(
    id: string,
    request: CancelPackageRequest
  ): Observable<CancelPackageResponse> {
    return this.http.post<CancelPackageResponse>(
      `${this.getPackageUrl(id)}/cancel`,
      request
    );
  }

  addAsset(
    packageId: string,
    request: AddPackageAssetRequest
  ): Observable<AddPackageAssetResponse> {
    return this.http.post<AddPackageAssetResponse>(
      `${this.getPackageUrl(packageId)}/assets`,
      request
    );
  }

  removeAsset(
    packageId: string,
    assetId: string
  ): Observable<RemovePackageAssetResponse> {
    return this.http.delete<RemovePackageAssetResponse>(
      `${this.getPackageUrl(packageId)}/assets/${encodeURIComponent(assetId)}`
    );
  }

  private getPackageUrl(id: string): string {
    return `${this.baseUrl}/${encodeURIComponent(id)}`;
  }
}