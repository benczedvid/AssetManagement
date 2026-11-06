window.runtimeConfig = {
  production: false,

  auth: {
    clientId: '',
    tenantId: '',
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin
  },

  api: {
    baseUrl: '/api',
    scope: ''
  }
};