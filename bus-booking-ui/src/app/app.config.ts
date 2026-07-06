import {
  ApplicationConfig,
  importProvidersFrom,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
  provideAppInitializer,
  inject,
} from '@angular/core';
import {
  provideRouter,
  withComponentInputBinding,
  withPreloading,
  PreloadAllModules,
  withViewTransitions,
} from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { MsalModule } from '@azure/msal-angular';
import {
  PublicClientApplication,
  InteractionType,
  LogLevel,
} from '@azure/msal-browser';

import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { API_BASE_URL } from './core/tokens/api-base-url.token';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { apiBaseInterceptor } from './core/interceptors/api-base.interceptor';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
import { timeoutInterceptor } from './core/interceptors/timeout.interceptor';
import { LocalAuthStrategy } from './core/services/local-auth-strategy';
import { AuthService } from './core/services/auth.service';

// MSAL is only enabled when a real clientId (not a placeholder) is configured.
const msalEnabled =
  !!environment.msal.clientId &&
  !environment.msal.clientId.startsWith('REPLACE_');

const msalProviders = msalEnabled
  ? (() => {
      const msalInstance = new PublicClientApplication({
        auth: {
          clientId: environment.msal.clientId,
          authority: `https://login.microsoftonline.com/${environment.msal.tenantId}`,
          redirectUri: environment.msal.redirectUri,
        },
        cache: { cacheLocation: 'sessionStorage' },
        system: {
          loggerOptions: {
            logLevel: environment.production ? LogLevel.Error : LogLevel.Warning,
          },
        },
      });
      return [
        importProvidersFrom(
          MsalModule.forRoot(
            msalInstance,
            {
              interactionType: InteractionType.Redirect,
              authRequest: { scopes: environment.msal.scopes },
            },
            {
              interactionType: InteractionType.Redirect,
              protectedResourceMap: new Map(),
            },
          ),
        ),
      ];
    })()
  : [];

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    // Block initial navigation/guards until MSAL redirect handling + local silent
    // refresh have settled — otherwise a hard reload on a protected deep link can
    // see isAuthenticated()===false and bounce to '/' before auth state resolves.
    provideAppInitializer(() => inject(AuthService).initialize()),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withViewTransitions(),
      withPreloading(PreloadAllModules),
    ),
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
    provideHttpClient(
      withInterceptors([
        errorInterceptor,
        authInterceptor,
        apiBaseInterceptor,
        retryInterceptor,
        timeoutInterceptor,
      ]),
    ),
    ...msalProviders,
    // LocalAuthStrategy is provided here (not providedIn: 'root') so it is absent
    // in test environments that don't include it, keeping the MSAL-only tests intact.
    ...(environment.localAuthEnabled ? [LocalAuthStrategy] : []),
  ],
};
