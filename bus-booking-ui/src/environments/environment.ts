export const environment = {
  production: true,
  apiBaseUrl: 'https://app-busbooking-prod-lbutwu.azurewebsites.net',
  localAuthEnabled: true,
  msal: {
    clientId: '4f3daaf0-2022-4bb5-8648-c091dba6f9e1',
    tenantId: '3e0c4033-baa5-4fea-8b67-f34175711849',
    redirectUri: 'https://app-busbooking-prod-lbutwu.azurewebsites.net',
    scopes: ['api://4f3daaf0-2022-4bb5-8648-c091dba6f9e1/user_impersonation'],
  },
};
