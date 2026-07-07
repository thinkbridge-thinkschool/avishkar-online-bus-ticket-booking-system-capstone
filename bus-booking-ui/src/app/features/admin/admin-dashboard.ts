import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminService, AdminDashboard } from '../../core/services/admin.service';
import { AuthService } from '../../core/services/auth.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';

@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink, DatePipe, LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboardComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  readonly auth = inject(AuthService);

  readonly dashboard = signal<AdminDashboard | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly today = new Date();

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
