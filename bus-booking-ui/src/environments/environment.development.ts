export const environment = {
  production: false,
  apiBaseUrl: '',  // empty → keeps URLs relative so the dev proxy forwards them
  msal: {
    clientId: 'cc1051c8-d4b5-49c9-a373-8780fb1c2a90',
    tenantId: '7e394fc8-4b86-4cfe-810e-43f86f8bec47',
    redirectUri: 'http://localhost:4200',
    scopes: ['api://cc1051c8-d4b5-49c9-a373-8780fb1c2a90/user_impersonation'],
  },
};
