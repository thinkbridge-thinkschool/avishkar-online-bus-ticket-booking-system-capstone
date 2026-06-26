import { Injectable, computed, inject, signal } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { AccountInfo, InteractionRequiredAuthError } from '@azure/msal-browser';
import { environment } from '../../../environments/environment';
import { LocalAuthStrategy } from './local-auth-strategy';

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Optional: null when MsalModule is not in the DI tree (e.g. test env without MSAL, or local-only).
  private readonly msal = inject(MsalService, { optional: true });
  // Optional: null when LocalAuthStrategy is not registered in app.config.ts.
  private readonly local = inject(LocalAuthStrategy, { optional: true });

  // ── MSAL state ─────────────────────────────────────────────────────────────
  private readonly _account = signal<AccountInfo | null>(null);

  // ── Computed signals ────────────────────────────────────────────────────────
  readonly isAuthenticated = computed(() =>
    this._account() !== null || (this.local?.isAuthenticated() ?? false),
  );

  readonly email = computed(() =>
    this._account()?.username ?? this.local?.email() ?? null,
  );

  readonly displayName = computed(() =>
    this._account()?.name ?? this.local?.displayName() ?? null,
  );

  readonly roles = computed<string[]>(() => {
    const msalRoles = (() => {
      const claims = this._account()?.idTokenClaims as Record<string, unknown> | undefined;
      const raw = claims?.['roles'];
      return Array.isArray(raw) ? (raw as string[]) : raw ? [String(raw)] : [];
    })();
    const localRoles = this.local?.roles() ?? [];
    // Merge without duplicates (MSAL user might also have local roles in future)
    return [...new Set([...msalRoles, ...localRoles])];
  });

  readonly isVendor     = computed(() => this.roles().includes('BusBooking.Vendor'));
  readonly isAdmin      = computed(() => this.roles().includes('BusBooking.Admin'));
  readonly isSuperAdmin = computed(() => this.roles().includes('BusBooking.SuperAdmin'));

  // ── Initialization ──────────────────────────────────────────────────────────

  async initialize(): Promise<void> {
    if (this.msal) {
      await this.msal.instance.initialize();
      await this.msal.instance.handleRedirectPromise().catch(() => null);
      const accounts = this.msal.instance.getAllAccounts();
      this._account.set(accounts[0] ?? null);
    }
    if (this.local) {
      await this.local.initialize();
    }
  }

  // ── Auth actions ─────────────────────────────────────────────────────────────

  // Default login: MSAL if configured, otherwise local login page.
  login(): void {
    if (this.msal) {
      this.msal.loginRedirect({ scopes: environment.msal.scopes });
    } else {
      this.local?.login();
    }
  }

  // Explicit local login — navigates to /login regardless of MSAL state.
  loginLocal(): void {
    this.local?.login();
  }

  logout(): void {
    if (this.local?.isAuthenticated()) {
      this.local.logout();
      return;
    }
    if (this.msal) {
      this.msal.logoutRedirect();
    }
  }

  // ── Token acquisition ─────────────────────────────────────────────────────────
  // Local JWT takes priority when the user authenticated via local auth.

  async getAccessToken(): Promise<string | null> {
    if (this.local?.isAuthenticated()) {
      return this.local.getAccessToken();
    }
    return this._acquireMsalToken(false);
  }

  async getAccessTokenForced(): Promise<string | null> {
    if (this.local?.isAuthenticated()) {
      return this.local.getAccessTokenForced();
    }
    return this._acquireMsalToken(true);
  }

  // Called by LocalLoginComponent after a successful /api/v1/auth/login response.
  setLocalAccessToken(token: string): void {
    this.local?.setAccessToken(token);
  }

  // ── Private helpers ───────────────────────────────────────────────────────────

  private async _acquireMsalToken(forceRefresh: boolean): Promise<string | null> {
    const account = this._account();
    if (!account || !this.msal) return null;
    try {
      const result = await this.msal.instance.acquireTokenSilent({
        scopes: environment.msal.scopes,
        account,
        forceRefresh,
      });
      return result.accessToken;
    } catch (err) {
      if (err instanceof InteractionRequiredAuthError) {
        await this.msal.instance.acquireTokenRedirect({
          scopes: environment.msal.scopes,
          account,
        });
      }
      return null;
    }
  }
}
