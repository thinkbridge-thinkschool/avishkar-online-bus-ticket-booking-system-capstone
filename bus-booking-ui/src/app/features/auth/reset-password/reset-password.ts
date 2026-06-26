import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { LocalAuthApiService } from '../../../core/services/local-auth-api.service';

@Component({
  selector: 'app-reset-password',
  imports: [FormsModule, RouterLink],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css',
})
export class ResetPasswordComponent implements OnInit {
  private readonly api    = inject(LocalAuthApiService);
  private readonly router = inject(Router);
  private readonly route  = inject(ActivatedRoute);

  private token = '';

  newPassword     = '';
  confirmPassword = '';

  readonly loading = signal(false);
  readonly error   = signal<string | null>(null);
  readonly success = signal(false);
  readonly invalidToken = signal(false);

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParams['token'] as string ?? '';
    if (!this.token) this.invalidToken.set(true);
  }

  async submit(): Promise<void> {
    if (this.newPassword.length < 8) {
      this.error.set('Password must be at least 8 characters.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.error.set('Passwords do not match.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.api.resetPassword(this.token, this.newPassword);
      this.success.set(true);
      setTimeout(() => void this.router.navigate(['/login']), 2500);
    } catch {
      this.error.set('Reset link is invalid or has expired. Request a new one.');
      this.invalidToken.set(true);
    } finally {
      this.loading.set(false);
    }
  }
}
