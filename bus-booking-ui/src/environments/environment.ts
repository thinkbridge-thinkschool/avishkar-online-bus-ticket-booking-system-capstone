export const environment = {
  production: true,
  apiBaseUrl: 'https://app-busbooking-prod-wa7imf.azurewebsites.net',
  localAuthEnabled: true,
  msal: {
    clientId: 'cc1051c8-d4b5-49c9-a373-8780fb1c2a90',
    tenantId: '7e394fc8-4b86-4cfe-810e-43f86f8bec47',
    redirectUri: 'https://app-busbooking-prod-wa7imf.azurewebsites.net',
    scopes: ['api://cc1051c8-d4b5-49c9-a373-8780fb1c2a90/user_impersonation'],
  },
};
