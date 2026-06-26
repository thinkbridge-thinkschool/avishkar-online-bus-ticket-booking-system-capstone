import { Component, inject, signal, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { LocalAuthApiService } from '../../../core/services/local-auth-api.service';

@Component({
  selector: 'app-local-register',
  imports: [FormsModule, RouterLink],
  templateUrl: './local-register.html',
  styleUrl: './local-register.css',
})
export class LocalRegisterComponent {
  private readonly auth   = inject(AuthService);
  private readonly api    = inject(LocalAuthApiService);
  private readonly router = inject(Router);

  email       = '';
  password    = '';
  displayName = '';

  readonly loading = signal(false);
  readonly error   = signal<string | null>(null);
  readonly success = signal(false);

  constructor() {
    effect(() => {
      if (this.auth.isAuthenticated()) {
        void this.router.navigate(['/']);
      }
    });
  }

  async submit(): Promise<void> {
    if (!this.email || !this.password || !this.displayName) {
      this.error.set('All fields are required.');
      return;
    }
    if (this.password.length < 8) {
      this.error.set('Password must be at least 8 characters.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.api.register(this.email.trim().toLowerCase(), this.password, this.displayName.trim());
      this.success.set(true);
    } catch (err: unknown) {
      const status = (err as { status?: number }).status;
      if (status === 409) {
        this.error.set('An account with this email already exists.');
      } else if (status === 400) {
        this.error.set('Please check your details and try again.');
      } else {
        this.error.set('Registration failed. Please try again.');
      }
    } finally {
      this.loading.set(false);
    }
  }
}
