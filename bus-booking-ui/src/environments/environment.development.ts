export const environment = {
  production: false,
  apiBaseUrl: '',  // empty → keeps URLs relative so the dev proxy forwards them
  localAuthEnabled: true,  // enable local email/password auth in development
  msal: {
    clientId: '4f3daaf0-2022-4bb5-8648-c091dba6f9e1',
    tenantId: '3e0c4033-baa5-4fea-8b67-f34175711849',
    redirectUri: 'http://localhost:4200',
    scopes: ['api://4f3daaf0-2022-4bb5-8648-c091dba6f9e1/user_impersonation'],
  },
};
