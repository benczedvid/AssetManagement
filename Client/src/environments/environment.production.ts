const runtimeConfig = window.runtimeConfig;

if (!runtimeConfig) {
  throw new Error(
    'Runtime configuration is missing. The assets/runtime-config.js file was not loaded.'
  );
}

if (!runtimeConfig.auth) {
  throw new Error(
    'Runtime configuration is invalid. The auth configuration is missing.'
  );
}

if (!runtimeConfig.api) {
  throw new Error(
    'Runtime configuration is invalid. The API configuration is missing.'
  );
}

if (!runtimeConfig.auth.clientId?.trim()) {
  throw new Error(
    'Runtime configuration is invalid. The frontend client ID is missing.'
  );
}

if (!runtimeConfig.auth.tenantId?.trim()) {
  throw new Error(
    'Runtime configuration is invalid. The tenant ID is missing.'
  );
}

if (!runtimeConfig.auth.redirectUri?.trim()) {
  throw new Error(
    'Runtime configuration is invalid. The redirect URI is missing.'
  );
}

if (!runtimeConfig.auth.postLogoutRedirectUri?.trim()) {
  throw new Error(
    'Runtime configuration is invalid. The post-logout redirect URI is missing.'
  );
}

if (!runtimeConfig.api.baseUrl?.trim()) {
  throw new Error(
    'Runtime configuration is invalid. The API base URL is missing.'
  );
}

if (!runtimeConfig.api.scope?.trim()) {
  throw new Error(
    'Runtime configuration is invalid. The API scope is missing.'
  );
}

export const environment = {
  production: runtimeConfig.production,

  auth: {
    clientId: runtimeConfig.auth.clientId.trim(),
    tenantId: runtimeConfig.auth.tenantId.trim(),
    redirectUri: runtimeConfig.auth.redirectUri.trim(),
    postLogoutRedirectUri:
      runtimeConfig.auth.postLogoutRedirectUri.trim()
  },

  api: {
    baseUrl: runtimeConfig.api.baseUrl.trim(),
    scope: runtimeConfig.api.scope.trim()
  }
} as const;