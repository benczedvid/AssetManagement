import { ApplicationConfig, importProvidersFrom, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { MSAL_GUARD_CONFIG, MSAL_INSTANCE, MsalBroadcastService, MsalService, MsalGuard, MSAL_INTERCEPTOR_CONFIG, MsalInterceptor } from '@azure/msal-angular'
import { routes } from './app.routes';
import { msalGuardConfigFactory, msalInstanceFactory, msalInterceptorConfigFactory } from './core/auth/auth.config';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";


export const appConfig: ApplicationConfig = {
  providers: [
    importProvidersFrom(MatSnackBarModule),
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: MSAL_INSTANCE, useFactory: msalInstanceFactory },
    { provide: MSAL_GUARD_CONFIG, useFactory: msalGuardConfigFactory },
    { provide: MSAL_INTERCEPTOR_CONFIG, useFactory: msalInterceptorConfigFactory },
    { provide: HTTP_INTERCEPTORS, useClass: MsalInterceptor, multi: true },
    MsalBroadcastService,
    MsalService,
    MsalGuard,

  ]
};
