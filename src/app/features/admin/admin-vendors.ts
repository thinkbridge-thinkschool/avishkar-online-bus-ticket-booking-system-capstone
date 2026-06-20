import { Component, OnInit, inject, signal } from '@angular/core';


import { AdminService } from '../../core/services/admin.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Vendor } from '../../shared/models/vendor.model';

@Component({
  selector: 'app-admin-vendors',
  imports: [LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './admin-vendors.html',
})
export class AdminVendorsComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  readonly vendors = signal<Vendor[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      this.vendors.set(await this.adminService.getAllVendors());
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async approve(vendorId: string): Promise<void> {
    try {
      await this.adminService.approveVendor(vendorId);
      this.vendors.update(list =>
        list.map(v => v.vendorId === vendorId ? { ...v, status: 'Approved' as const } : v)
      );
    } catch (err: unknown) {
      this.actionError.set((err as Error).message);
    }
  }

  async reject(vendorId: string): Promise<void> {
    if (!confirm('Reject this vendor?')) return;
    try {
      await this.adminService.rejectVendor(vendorId);
      this.vendors.update(list =>
        list.map(v => v.vendorId === vendorId ? { ...v, status: 'Rejected' as const } : v)
      );
    } catch (err: unknown) {
      this.actionError.set((err as Error).message);
    }
  }
}
