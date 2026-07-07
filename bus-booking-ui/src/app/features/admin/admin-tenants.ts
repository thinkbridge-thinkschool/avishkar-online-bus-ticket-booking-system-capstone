import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TenantService } from '../../core/services/tenant.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Tenant } from '../../shared/models/tenant.model';

@Component({
  selector: 'app-admin-tenants',
  imports: [RouterLink, LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './admin-tenants.html',
})
export class AdminTenantsComponent implements OnInit {
  private readonly tenantService = inject(TenantService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly tenants = signal<Tenant[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      this.tenants.set(await this.tenantService.getAllTenants());
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async approve(tenantId: string): Promise<void> {
    this.actionError.set(null);
    try {
      await this.tenantService.approveTenant(tenantId);
      this.tenants.update(list =>
        list.map(t => t.tenantId === tenantId ? { ...t, status: 'Active' as const } : t),
      );
    } catch (err: unknown) {
      this.actionError.set((err as Error).message);
    }
  }

  async reject(tenantId: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Confirm Reject',
      message: 'Are you sure you want to reject this tenant registration?',
      confirmText: 'Reject',
      danger: true,
    });
    if (!confirmed) return;
    this.actionError.set(null);
    try {
      await this.tenantService.rejectTenant(tenantId);
      this.tenants.update(list =>
        list.map(t => t.tenantId === tenantId ? { ...t, status: 'Rejected' as const } : t),
      );
    } catch (err: unknown) {
      this.actionError.set((err as Error).message);
    }
  }

  async suspend(tenantId: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Confirm Suspend',
      message: 'Suspend this tenant? Their users will lose access.',
      confirmText: 'Suspend',
      danger: true,
    });
    if (!confirmed) return;
    this.actionError.set(null);
    try {
      await this.tenantService.suspendTenant(tenantId);
      this.tenants.update(list =>
        list.map(t => t.tenantId === tenantId ? { ...t, status: 'Suspended' as const } : t),
      );
    } catch (err: unknown) {
      this.actionError.set((err as Error).message);
    }
  }

  async reactivate(tenantId: string): Promise<void> {
    this.actionError.set(null);
    try {
      await this.tenantService.reactivateTenant(tenantId);
      this.tenants.update(list =>
        list.map(t => t.tenantId === tenantId ? { ...t, status: 'Active' as const } : t),
      );
    } catch (err: unknown) {
      this.actionError.set((err as Error).message);
    }
  }
}
