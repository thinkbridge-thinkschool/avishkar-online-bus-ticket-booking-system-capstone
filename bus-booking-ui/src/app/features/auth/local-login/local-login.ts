import { Component, inject, signal, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { LocalAuthApiService } from '../../../core/services/local-auth-api.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-local-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './local-login.html',
  styleUrl: './local-login.css',
})
export class LocalLoginComponent {
  private readonly auth   = inject(AuthService);
  private readonly api    = inject(LocalAuthApiService);
  private readonly router = inject(Router);
  private readonly route  = inject(ActivatedRoute);

  readonly msalEnabled = !!environment.msal.clientId &&
                         !environment.msal.clientId.startsWith('REPLACE_');

  email    = '';
  password = '';

  readonly loading = signal(false);
  readonly error   = signal<string | null>(null);

  constructor() {
    effect(() => {
      if (this.auth.isAuthenticated()) {
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] as string | undefined;
        const target = returnUrl ?? (this.auth.isAnyAdmin() ? '/admin/dashboard' : '/');
        void this.router.navigateByUrl(target);
      }
    });
  }

  async submit(): Promise<void> {
    if (!this.email || !this.password) {
      this.error.set('Please enter your email and password.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    try {
      const response = await this.api.login(this.email.trim().toLowerCase(), this.password);
      this.auth.setLocalAccessToken(response.accessToken);
    } catch (err: unknown) {
      const status = (err as { status?: number }).status;
      if (status === 401) {
        this.error.set('Invalid email or password.');
      } else if (status === 423) {
        this.error.set('Account is temporarily locked due to too many failed attempts. Try again in 15 minutes.');
      } else if (status === 403) {
        this.error.set('Email address not verified. Check your inbox for a verification link.');
      } else {
        this.error.set('Sign-in failed. Please try again.');
      }
    } finally {
      this.loading.set(false);
    }
  }

  loginWithMicrosoft(): void {
    this.auth.login();
  }
}
