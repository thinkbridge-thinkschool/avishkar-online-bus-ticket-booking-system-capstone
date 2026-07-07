import { Component, OnInit, inject, signal } from '@angular/core';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { AdminService } from '../../core/services/admin.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import type { Vendor } from '../../shared/models/vendor.model';

@Component({
  selector: 'app-admin-pending-vendors',
  imports: [LoadingSpinnerComponent],
  templateUrl: './admin-pending-vendors.html',
})
export class AdminPendingVendorsComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly vendors = signal<Vendor[]>([]);
  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    try {
      this.vendors.set(await this.adminService.getPendingVendors());
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
      this.refreshing.set(false);
    }
  }

  async refresh(): Promise<void> {
    this.refreshing.set(true);
    this.error.set(null);
    await this.load();
  }

  async approve(vendorId: string): Promise<void> {
    try {
      await this.adminService.approveVendor(vendorId);
      this.vendors.update(list => list.filter(v => v.vendorId !== vendorId));
    } catch (err: unknown) {
      this.actionError.set((err as Error).message);
    }
  }

  async reject(vendorId: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Confirm Reject',
      message: 'Are you sure you want to reject this vendor?',
      confirmText: 'Reject',
      danger: true,
    });
    if (!confirmed) return;
    try {
      await this.adminService.rejectVendor(vendorId);
      this.vendors.update(list => list.filter(v => v.vendorId !== vendorId));
    } catch (err: unknown) {
      this.actionError.set((err as Error).message);
    }
  }
}
