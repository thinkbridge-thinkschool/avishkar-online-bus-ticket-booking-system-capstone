import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LocalAuthApiService } from '../../../core/services/local-auth-api.service';

@Component({
  selector: 'app-forgot-password',
  imports: [FormsModule, RouterLink],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPasswordComponent {
  private readonly api = inject(LocalAuthApiService);

  email = '';

  readonly loading = signal(false);
  readonly sent    = signal(false);

  async submit(): Promise<void> {
    if (!this.email) return;
    this.loading.set(true);
    try {
      await this.api.forgotPassword(this.email.trim().toLowerCase());
      this.sent.set(true);
    } catch {
      // Always show success to prevent user enumeration (API also returns 200).
      this.sent.set(true);
    } finally {
      this.loading.set(false);
    }
  }
}
