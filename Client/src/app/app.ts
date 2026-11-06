import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import { AccountInfo, InteractionStatus } from '@azure/msal-browser';
import { filter, Subject, take, takeUntil } from 'rxjs';
import { CurrentUserApi } from './features/auth/current-user-api';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
  ],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit, OnDestroy {
  private readonly authService = inject(MsalService);
  private readonly msalBroadcastService = inject(MsalBroadcastService);
  private readonly currentUserApi = inject(CurrentUserApi);
  private readonly router = inject(Router);
  private readonly destroy$ = new Subject<void>();
  readonly isInitializingUser = signal(false);
  readonly initializationError = signal<string | null>(null);

  ngOnInit(): void {
    this.handleAuthenticationRedirect();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private handleAuthenticationRedirect(): void {
    this.authService
      .handleRedirectObservable()
      .pipe(
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: result => {
          if (result?.account) {
            this.authService.instance.setActiveAccount(
              result.account
            );
          }

          this.waitForAuthenticationCompletion();
        },

        error: error => {
          console.error('The authentication response could not be processed.', error);
          this.initializationError.set('The authentication response could not be processed.');
        },
      });
  }

  private waitForAuthenticationCompletion(): void {
    this.msalBroadcastService.inProgress$
      .pipe(
        filter(
          status =>  status === InteractionStatus.None
        ),
        take(1),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        const activeAccount = this.getOrSetActiveAccount();
        if (!activeAccount) {
          return;
        }

        this.initializeCurrentUser();
      });
  }

  private getOrSetActiveAccount(): AccountInfo | null {
    const activeAccount = this.authService.instance.getActiveAccount();

    if (activeAccount) {
      return activeAccount;
    }

    const accounts = this.authService.instance.getAllAccounts();
    const firstAccount = accounts[0];

    if (!firstAccount) {
      return null;
    }

    this.authService.instance.setActiveAccount(firstAccount);

    return firstAccount;
  }

  private initializeCurrentUser(): void {
    if (this.isInitializingUser()) {
      return;
    }

    this.isInitializingUser.set(true);
    this.initializationError.set(null);

    this.currentUserApi
      .getOrCreateCurrentUser()
      .pipe(
        take(1),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: user => {
          this.isInitializingUser.set(false);

          if (!user.isActive) {
            this.initializationError.set('The user account is inactive.');

            return;
          }

        },

        error: error => {
          this.isInitializingUser.set(false);

          console.error('The creation or loading of the local user profile failed.', error);

          this.initializationError.set(
            this.getInitializationErrorMessage(error?.status)
          );
        },
      });
  }



  private getCurrentUrlWithoutQueryOrFragment(): string {
    return this.router.url
      .split('?')[0]
      .split('#')[0];
  }

  private getInitializationErrorMessage(
    status: number | undefined): string {
    switch (status) {
      case 401:
        return 'Authentication succeeded, but the user information in the token is invalid or incomplete.';
      case 403:
        return 'Authentication succeeded, but you do not have the required API permission.';
      case 0:
        return 'The AssetManagement API is unavailable.';

      default:
        return 'Authentication succeeded, but the local user profile could not be created or loaded.';
    }
  }
}
