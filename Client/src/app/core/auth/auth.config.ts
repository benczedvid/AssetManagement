import { BrowserCacheLocation, Configuration, IPublicClientApplication, PublicClientApplication, InteractionType } from '@azure/msal-browser';
import { MsalGuardConfiguration, MsalInterceptorConfiguration } from '@azure/msal-angular';
import { environment } from '../../../environments/environment';

const msalConfig: Configuration = {
  auth: {
    clientId: environment.azureAd.clientId,
    authority: `https://login.microsoftonline.com/${environment.azureAd.tenantId}`,
    redirectUri: environment.azureAd.redirectUri,
    postLogoutRedirectUri: environment.azureAd.postLogoutRedirectUri
  },

  cache: {
    cacheLocation: BrowserCacheLocation.SessionStorage
  },

  system: {
    allowPlatformBroker: false
  }
};

export function msalInstanceFactory(): IPublicClientApplication {
  return new PublicClientApplication(msalConfig);
}
export function msalGuardConfigFactory():
  MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: {
      scopes: [
        'openid',
        'profile',
        'email'
      ]
    },
    loginFailedRoute: '/login'
  };
}

export function msalInterceptorConfigFactory():
  MsalInterceptorConfiguration {
  const protectedResourceMap = new Map<string, Array<string> | null>();
  protectedResourceMap.set(`${environment.api.baseUrl}/asset-movement`, null);
  protectedResourceMap.set(`${environment.api.baseUrl}/*`, [environment.api.scope]);
  return {
    interactionType: InteractionType.Redirect, protectedResourceMap
  };
}
