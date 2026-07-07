import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../core/services/admin.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Vendor } from '../../shared/models/vendor.model';

@Component({
  selector: 'app-admin-vendors',
  imports: [RouterLink, LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './admin-vendors.html',
  styleUrl: './admin-vendors.css',
})
export class AdminVendorsComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly vendors = signal<Vendor[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly searchTerm = signal('');

  readonly filteredVendors = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return this.vendors();
    return this.vendors().filter(v =>
      v.vendorName.toLowerCase().includes(term) || v.email.toLowerCase().includes(term)
    );
  });

  async ngOnInit(): Promise<void> {
    try {
      this.vendors.set(await this.adminService.getAllVendors());
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  updateSearch(value: string): void {
    this.searchTerm.set(value);
  }

  async deactivate(vendorId: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Confirm Deactivate',
      message: 'Deactivate this vendor? They will no longer be able to manage buses or schedules.',
      confirmText: 'Deactivate',
      danger: true,
    });
    if (!confirmed) return;
    try {
      await this.adminService.deactivateVendor(vendorId);
      this.vendors.update(list =>
        list.map(v => v.vendorId === vendorId ? { ...v, isActive: false } : v)
      );
    } catch (err: unknown) {
      this.actionError.set((err as Error).message);
    }
  }
}
