import { Injectable, computed, signal } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { AccountInfo, InteractionRequiredAuthError } from '@azure/msal-browser';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _account = signal<AccountInfo | null>(null);
  private readonly _token = signal<string | null>(null);

  readonly isAuthenticated = computed(() => this._account() !== null);
  readonly email = computed(() => this._account()?.username ?? null);
  readonly displayName = computed(() => this._account()?.name ?? null);
  readonly roles = computed<string[]>(() => {
    const claims = this._account()?.idTokenClaims as Record<string, unknown> | undefined;
    const raw = claims?.['roles'];
    return Array.isArray(raw) ? (raw as string[]) : raw ? [String(raw)] : [];
  });
  readonly isVendor = computed(() => this.roles().includes('BusBooking.Vendor'));
  readonly isAdmin = computed(() => this.roles().includes('BusBooking.Admin'));
  readonly isSuperAdmin = computed(() => this.roles().includes('BusBooking.SuperAdmin'));

  constructor(private readonly msal: MsalService) {}

  initialize(): void {
    this.msal.instance.initialize().then(() => {
      this.msal.instance.handleRedirectPromise()
        .catch(() => null)  // swallow redirect errors; account loaded below regardless
        .finally(() => {
          const accounts = this.msal.instance.getAllAccounts();
          this._account.set(accounts[0] ?? null);
        });
    });
  }

  login(): void {
    this.msal.loginRedirect({ scopes: environment.msal.scopes });
  }

  logout(): void {
    this.msal.logoutRedirect();
  }

  async getAccessToken(): Promise<string | null> {
    return this.acquireToken(false);
  }

  async getAccessTokenForced(): Promise<string | null> {
    return this.acquireToken(true);
  }

  private async acquireToken(forceRefresh: boolean): Promise<string | null> {
    const account = this._account();
    if (!account) return null;

    try {
      const result = await this.msal.instance.acquireTokenSilent({
        scopes: environment.msal.scopes,
        account,
        forceRefresh,
      });
      this._token.set(result.accessToken);
      return result.accessToken;
    } catch (err) {
      if (err instanceof InteractionRequiredAuthError) {
        // Consent not yet granted or token expired — redirect to Entra ID.
        // After the user grants consent they are redirected back and the
        // next acquireTokenSilent call succeeds without interaction.
        await this.msal.instance.acquireTokenRedirect({
          scopes: environment.msal.scopes,
          account,
        });
      }
      return null;
    }
  }
}
