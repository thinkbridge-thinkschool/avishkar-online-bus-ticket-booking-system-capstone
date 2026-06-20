export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5084',
  msal: {
    clientId: 'REPLACE_WITH_CLIENT_ID',
    tenantId: 'REPLACE_WITH_TENANT_ID',
    redirectUri: 'http://localhost:4200',
    scopes: ['api://BusBooking/.default'],
  },
};
