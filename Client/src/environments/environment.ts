export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:5252/api',

  auth: {
    clientId: 'ec2045a3-e691-4e7f-8fa2-cbc5f9b47ac3',
    tenantId: '60e4a2b6-76d1-427f-b009-d36e044bac1c',
      redirectUri: 'http://localhost:4200',
      postLogoutRedirectUri: 'http://localhost:4200/login'
  },
  api: {
    baseUrl: 'https://localhost:5252/api',
    scope: 'api://35fc6781-4984-4ad8-b2d7-afa686be35fc/access_as_user'
  }
  };
