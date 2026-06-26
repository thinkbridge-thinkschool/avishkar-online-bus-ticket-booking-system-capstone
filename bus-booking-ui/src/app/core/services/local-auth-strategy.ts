import { Injectable, Signal, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LocalAuthApiService } from './local-auth-api.service';

interface LocalJwtPayload {
  'app:userId': string;
  email: string;
  name: string;
  roles?: string | string[];
  exp: number;
}

function parseJwt(token: string): LocalJwtPayload | null {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(base64)) as LocalJwtPayload;
  } catch {
    return null;
  }
}

// NOT providedIn: 'root' — provided explicitly in app.config.ts when localAuth is enabled.
// AuthService injects this with { optional: true } so tests without this provider
// receive null and continue using the MSAL-only path unchanged.
@Injectable()
export class LocalAuthStrategy {
  private readonly _accessToken = signal<string | null>(null);

  private readonly _payload = computed<LocalJwtPayload | null>(() => {
    const t = this._accessToken();
    return t ? parseJwt(t) : null;
  });

  readonly isAuthenticated: Signal<boolean> = computed(() => this._accessToken() !== null);
  readonly email: Signal<string | null> = computed(() => this._payload()?.email ?? null);
  readonly displayName: Signal<string | null> = computed(() => this._payload()?.name ?? null);
  readonly roles: Signal<string[]> = computed(() => {
    const raw = this._payload()?.roles;
    if (!raw) return [];
    return Array.isArray(raw) ? raw : [raw];
  });

  constructor(
    private readonly api: LocalAuthApiService,
    private readonly router: Router,
  ) {}

  // Called by AuthService.initialize(). Attempts a silent refresh so a page
  // reload doesn't force re-login when a refresh token cookie is present.
  async initialize(): Promise<void> {
    try {
      const response = await this.api.refresh();
      this._accessToken.set(response.accessToken);
    } catch {
      // No valid session — user must log in explicitly.
    }
  }

  // Called by LocalAuthComponent after a successful login response.
  setAccessToken(token: string): void {
    this._accessToken.set(token);
  }

  login(): void {
    this.router.navigate(['/login']);
  }

  logout(): void {
    const prev = this._accessToken();
    this._accessToken.set(null);
    if (prev) {
      this.api.logout().catch(() => void 0);
    }
    this.router.navigate(['/']);
  }

  getAccessToken(): string | null {
    return this._accessToken();
  }

  async getAccessTokenForced(): Promise<string | null> {
    try {
      const response = await this.api.refresh();
      this._accessToken.set(response.accessToken);
      return response.accessToken;
    } catch {
      this._accessToken.set(null);
      return null;
    }
  }
}
