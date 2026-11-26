import { PackageListItem } from './package-list-item';

export interface GetPackagesResponse {
  readonly items: readonly PackageListItem[];
}