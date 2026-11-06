import { inject } from '@angular/core';

import {
  CanActivateFn,
  Router,
  UrlTree
} from '@angular/router';

import {
  catchError,
  map,
  Observable,
  of
} from 'rxjs';

import { CurrentUserApi } from
  '../current-user-api';

import { ApplicationPermission } from
  '../authorization/application-permission';

export const permissionGuard: CanActivateFn = (
  route
): boolean | UrlTree | Observable<boolean | UrlTree> => {
  const currentUserApi = inject(CurrentUserApi);
  const router = inject(Router);

  const requiredPermissions =
    route.data['permissions'] as
      readonly ApplicationPermission[] | undefined;

  if (
    !requiredPermissions ||
    requiredPermissions.length === 0
  ) {
    console.error(
      'The protected route does not define any required permissions.'
    );

    return router.createUrlTree([
      '/forbidden'
    ]);
  }

  const currentUser = currentUserApi.currentUser();

  if (currentUser) {
    const isAllowed =
      currentUser.isActive &&
      currentUserApi.hasAnyPermission(
        requiredPermissions
      );

    if (!isAllowed) {
      logAuthorizationFailure(
        currentUser.role,
        requiredPermissions
      );
    }

    return isAllowed
      ? true
      : router.createUrlTree([
          '/forbidden'
        ]);
  }

  return currentUserApi
    .getOrCreateCurrentUser()
    .pipe(
      map(user => {
        const isAllowed =
          user.isActive &&
          currentUserApi.hasAnyPermission(
            requiredPermissions
          );

        if (!isAllowed) {
          logAuthorizationFailure(
            user.role,
            requiredPermissions
          );
        }

        return isAllowed
          ? true
          : router.createUrlTree([
              '/forbidden'
            ]);
      }),

      catchError(error => {
        console.error(
          'The current user could not be loaded while authorizing the route.',
          {
            status: error?.status,
            statusText: error?.statusText,
            url: error?.url,
            error: error?.error ?? error
          }
        );

        return of(
          router.createUrlTree([
            '/forbidden'
          ])
        );
      })
    );
};

function logAuthorizationFailure(
  role: string,
  requiredPermissions:
    readonly ApplicationPermission[]
): void {
  console.error(
    'The current user does not have permission to activate the route.',
    {
      role,
      requiredPermissions
    }
  );
}