export {};

declare global {
  interface RuntimeAuthConfig {
    clientId: string;
    tenantId: string;
    redirectUri: string;
    postLogoutRedirectUri: string;
  }

  interface RuntimeApiConfig {
    baseUrl: string;
    scope: string;
  }

  interface RuntimeConfig {
    production: boolean;
    auth: RuntimeAuthConfig;
    api: RuntimeApiConfig;
  }

  interface Window {
    runtimeConfig?: RuntimeConfig;
  }
}