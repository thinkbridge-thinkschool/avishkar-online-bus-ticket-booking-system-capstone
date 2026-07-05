import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface TokenResponse {
  accessToken: string;
  expiresIn: number;
  tokenType: 'Bearer';
}

export interface RegisterResponse {
  message: string;
  userId: string;
}

@Injectable({ providedIn: 'root' }) // Create only one object of LocalAuthApiService and make it available throughout the application.
export class LocalAuthApiService {
  constructor(private readonly http: HttpClient) {}            // HttpClient is Angular's built-in class for calling APIs.

  register(email: string, password: string, displayName: string) { // Create HttpRequest so send to interceptor chain in app.config.ts
    return firstValueFrom(
      this.http.post<RegisterResponse>('/api/v1/auth/register', { email, password, displayName }), //It tells Angular: The backend will return data matching the RegisterResponse type
    ); // Angular lets the API Base Interceptor add the base URL later. 
  }

  login(email: string, password: string) {
    return firstValueFrom(
      this.http.post<TokenResponse>('/api/v1/auth/login', { email, password }, { withCredentials: true }), //This sends an HTTP POST request. Why POST? Because we are sending data to the server.
    ); //  withCredentials: true, Refresh Token cookie can be stored or sent. 2) This request may use cookies
  }

  refresh() {
    return firstValueFrom(
      this.http.post<TokenResponse>('/api/v1/auth/refresh', {}, { withCredentials: true }),
    );
  }

  logout() {
    return firstValueFrom(
      this.http.post<void>('/api/v1/auth/logout', {}, { withCredentials: true }),
    );
  }

  verifyEmail(token: string) {
    return firstValueFrom(
      this.http.get<{ message: string }>(`/api/v1/auth/verify-email?token=${encodeURIComponent(token)}`),
    );
  }

  forgotPassword(email: string) {
    return firstValueFrom(
      this.http.post<{ message: string }>('/api/v1/auth/forgot-password', { email }),
    );
  }

  resetPassword(token: string, newPassword: string) {
    return firstValueFrom(
      this.http.post<{ message: string }>('/api/v1/auth/reset-password', { token, newPassword }),
    );
  }

  // ── Account linking ─────────────────────────────────────────────────────────

  getLinkedAccounts() {
    return firstValueFrom(
      this.http.get<{ provider: string; linkedAt: string }[]>('/api/v1/users/me/linked-accounts'),
    );
  }

  linkLocal(password: string) {
    return firstValueFrom(
      this.http.post<void>('/api/v1/users/me/link-local', { password }),
    );
  }

  unlinkProvider(provider: string) {
    return firstValueFrom(
      this.http.delete<void>(`/api/v1/users/me/linked-accounts/${provider}`),
    );
  }
}
