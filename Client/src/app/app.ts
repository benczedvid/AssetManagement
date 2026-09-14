import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import { AuthenticationResult, InteractionStatus } from '@azure/msal-browser';
import { filter, Subject, take, takeUntil, } from 'rxjs';
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
  private authenticationResult: AuthenticationResult | null = null;

  private readonly destroy$ = new Subject<void>();

  readonly isInitializingUser = signal(false);
  readonly initializationError =  signal<string | null>(null);

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
          this.setActiveAccount(result);
          this.waitForAuthenticationCompletion();
        },

        error: error => {
          console.error(
            'The authentication response could not be processed.',
            error
          );

          this.initializationError.set(
            'The authentication response could not be processed.'
          );
        },
      });
  }

  private waitForAuthenticationCompletion(): void {
    this.msalBroadcastService.inProgress$
      .pipe(
        filter(
          status =>
            status === InteractionStatus.None
        ),
        take(1),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        const activeAccount =
          this.getOrSetActiveAccount();

        if (!activeAccount) {
          return;
        }

        this.initializeCurrentUser();
      });
  }

  private setActiveAccount(result: AuthenticationResult | null): void {
    if (!result?.account) {
      return;
    }

    this.authService.instance.setActiveAccount(
      result.account
    );
  }

  private getOrSetActiveAccount() {
    const activeAccount =
      this.authService.instance.getActiveAccount();

    if (activeAccount) {
      return activeAccount;
    }

    const accounts =
      this.authService.instance.getAllAccounts();

    const firstAccount = accounts[0];

    if (!firstAccount) {
      return null;
    }

    this.authService.instance.setActiveAccount(
      firstAccount
    );

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
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: user => {
          this.isInitializingUser.set(false);

          if (!user.isActive) {
            this.initializationError.set(
              'The user account is inactive.'
            );

            return;
          }

          this.navigateAfterAuthentication();
        },

        error: error => {
          this.isInitializingUser.set(false);

          console.error(
            'The creation or loading of the local user profile failed.',
            error
          );

          this.initializationError.set(
            this.getInitializationErrorMessage(
              error.status
            )
          );
        },
      });
  }
  private navigateAfterAuthentication(): void {

    const currentUrl = this.getCurrentUrlWithoutQueryOrFragment();
    if(this.authenticationResult === null){
      return;
    }
    if (currentUrl !== '/' && currentUrl !== '/login)'){
      return;
    }
    void this.router.navigate(['/dashboard'], {
      replaceUrl: true,
    }
    );
  }
  private getCurrentUrlWithoutQueryOrFragment(): string {
    return this.router.url.split('?')[0].split('#')[0];
  }

  private getInitializationErrorMessage(
    status: number
  ): string {
    switch (status) {
      case 401:
        return 'A bejelentkezés megtörtént, de a token felhasználói adatai érvénytelenek vagy hiányosak.';

      case 403:
        return 'A bejelentkezés megtörtént, de nincs megfelelő API-jogosultságod.';

      case 0:
        return 'A DeathStar API nem érhető el.';

      default:
        return 'A bejelentkezés sikerült, de a felhasználói profil nem hozható létre.';
    }
  }
}
