import { Component, inject, signal } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { RedirectRequest } from '@azure/msal-browser';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-login',
  imports: [],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly authService = inject(MsalService);

  readonly isLoginInProgress = signal(false);
  readonly loginError = signal<string | null>(null);

  login(): void {
    if (this.isLoginInProgress()) {
      return;
    }

    this.isLoginInProgress.set(true);
    this.loginError.set(null);

    const loginRequest: RedirectRequest = {
      scopes: [
        'openid',
        'profile',
        'email',
        environment.api.scope,
      ],
    };

    this.authService
      .loginRedirect(loginRequest)
      .subscribe({
        error: error => {
          console.error(
            'Login could not be started.',
            error
          );

          this.loginError.set(
            'Login could not be started.'
          );

          this.isLoginInProgress.set(false);
        },
      });
  }
}
