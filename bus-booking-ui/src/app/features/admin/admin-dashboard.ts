import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminService, AdminDashboard } from '../../core/services/admin.service';
import { AuthService } from '../../core/services/auth.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink, LoadingSpinnerComponent],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboardComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  readonly auth = inject(AuthService);

  readonly dashboard = signal<AdminDashboard | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      this.dashboard.set(await this.adminService.getDashboard());
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }
}
