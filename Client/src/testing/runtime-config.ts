// Test-only runtime configuration, loaded as a karma polyfill so that
// src/environments/environment.ts can be imported by specs.
window.__RUNTIME_CONFIG__ = {
  production: false,
  azureAd: {
    clientId: 'test-client-id',
    tenantId: 'test-tenant-id',
    authority: 'https://login.microsoftonline.com/test-tenant-id',
    redirectUri: 'http://localhost:4200',
    postLogoutRedirectUri: 'http://localhost:4200',
  },
  api: {
    baseUrl: 'http://localhost/api',
    scope: 'api://test/access_as_user',
  },
};
