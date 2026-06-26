import { Component, inject, effect } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-signup',
  imports: [RouterLink],
  templateUrl: './signup.html',
  styleUrl: './signup.css',
})
export class SignupComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly msalEnabled     = !!environment.msal.clientId &&
                              !environment.msal.clientId.startsWith('REPLACE_');
  readonly localAuthEnabled = environment.localAuthEnabled;

  constructor() {
    effect(() => {
      if (this.auth.isAuthenticated()) {
        void this.router.navigate(['/']);
      }
    });
  }

  signup(): void {
    this.auth.login();
  }

  signin(): void {
    this.auth.login();
  }
}
