export const environment = {
  production: true,
  apiBaseUrl: 'https://app-busbooking-prod-wa7imf.azurewebsites.net',
  localAuthEnabled: false, // enable when local auth is ready for production
  msal: {
    clientId: 'REPLACE_WITH_CLIENT_ID',
    tenantId: 'REPLACE_WITH_TENANT_ID',
    redirectUri: 'https://app-busbooking-prod-wa7imf.azurewebsites.net',
    scopes: ['api://BusBooking/.default'],
  },
};
