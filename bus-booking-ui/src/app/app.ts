import { Component, OnInit, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { NavBarComponent } from './shared/components/nav-bar/nav-bar';
import { ConfirmDialogComponent } from './shared/components/confirm-dialog/confirm-dialog';
import { AssistantChatComponent } from './shared/components/assistant-chat/assistant-chat';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavBarComponent, ConfirmDialogComponent, AssistantChatComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  // The admin section has its own sidebar shell (AdminLayoutComponent) and
  // deliberately replaces the customer-facing top navbar rather than stacking both.
  readonly isAdminRoute = signal(this.router.url.startsWith('/admin'));

  ngOnInit(): void {
    // MSAL redirects back to "/" with no dedicated callback route — send admins
    // straight to their dashboard instead of leaving them on the customer home page.
    if (this.auth.consumeRedirectFlag() && this.auth.isAnyAdmin()) {
      void this.router.navigateByUrl('/admin/dashboard');
    }

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(e => this.isAdminRoute.set(e.urlAfterRedirects.startsWith('/admin')));
  }
}
