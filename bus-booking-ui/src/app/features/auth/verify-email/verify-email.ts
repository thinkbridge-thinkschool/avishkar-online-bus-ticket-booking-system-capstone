import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { LocalAuthApiService } from '../../../core/services/local-auth-api.service';

@Component({
  selector: 'app-verify-email',
  imports: [RouterLink],
  templateUrl: './verify-email.html',
  styleUrl: './verify-email.css',
})
export class VerifyEmailComponent implements OnInit {
  private readonly api   = inject(LocalAuthApiService);
  private readonly route = inject(ActivatedRoute);

  readonly status = signal<'loading' | 'success' | 'error'>('loading');
  readonly message = signal('');

  async ngOnInit(): Promise<void> {
    const token = this.route.snapshot.queryParams['token'] as string | undefined;
    if (!token) {
      this.message.set('Verification link is missing or invalid.');
      this.status.set('error');
      return;
    }
    try {
      await this.api.verifyEmail(token);
      this.message.set('Your email has been verified. You can now sign in.');
      this.status.set('success');
    } catch {
      this.message.set('Verification link is invalid or has expired. Request a new one by registering again.');
      this.status.set('error');
    }
  }
}
