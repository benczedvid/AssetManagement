interface RuntimeConfiguration {
  production: boolean;

  azureAd: {
    clientId: string;
    tenantId: string;
    authority: string;
    redirectUri: string;
    postLogoutRedirectUri: string;
  };

  api: {
    baseUrl: string;
    scope: string;
  };
}

declare global {
  interface Window {
    __RUNTIME_CONFIG__?: RuntimeConfiguration;
  }
}

const runtimeConfiguration = window.__RUNTIME_CONFIG__;

if (!runtimeConfiguration) {
  throw new Error('Frontend runtime configuration could not be loaded.');
}

if (  !runtimeConfiguration.azureAd.clientId?.trim()) {
  throw new Error('Frontend Azure AD client ID is missing.');
}

if (!runtimeConfiguration.azureAd.tenantId?.trim()) {
  throw new Error('Frontend Azure AD tenant ID is missing.');
}

if (!runtimeConfiguration.api.scope?.trim()) {
  throw new Error('Frontend API scope is missing.'
  );
}

export const environment = {
  production:
    runtimeConfiguration.production,

  azureAd: {
    clientId: runtimeConfiguration.azureAd.clientId,
    tenantId: runtimeConfiguration.azureAd.tenantId,
    authority: runtimeConfiguration.azureAd.authority,
    redirectUri: runtimeConfiguration.azureAd.redirectUri,
    postLogoutRedirectUri: runtimeConfiguration.azureAd.postLogoutRedirectUri,
  },

  api: {
    baseUrl: runtimeConfiguration.api.baseUrl,
    scope: runtimeConfiguration.api.scope,
  },
};