const runtimeConfig = window.runtimeConfig;

export const environment = {
  production: runtimeConfig?.production ?? false,

  auth: {
    clientId: runtimeConfig?.auth.clientId ?? '',
    tenantId: runtimeConfig?.auth.tenantId ?? '',
    redirectUri:
      runtimeConfig?.auth.redirectUri ??
      window.location.origin,
    postLogoutRedirectUri:
      runtimeConfig?.auth.postLogoutRedirectUri ??
      window.location.origin
  },

  api: {
    baseUrl: runtimeConfig?.api.baseUrl ?? '/api',
    scope: runtimeConfig?.api.scope ?? ''
  }
}