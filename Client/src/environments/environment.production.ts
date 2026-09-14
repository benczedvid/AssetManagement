export const environment = {
  production: true,

  auth: {
    clientId: 'FRONTEND_APPLICATION_CLIENT_ID',
    tenantId: 'TENANT_ID',
    redirectUri: 'PRODUCTION_REDIRECT_URI',
    postLogoutRedirectUri: 'PRODUCTION_LOGOUT_REDIRECT_URI'
  },
  api: {
    baseUrl: 'PRODUCTION_API_BASE_URL',
    scope: 'api://PRODUCTION_API_SCOPE/access_as_user'
  }
};
