import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { VendorService } from '../../core/services/vendor.service';
import { ScheduleService } from '../../core/services/schedule.service';
import { AuthService } from '../../core/services/auth.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Vendor } from '../../shared/models/vendor.model';
import type { Schedule } from '../../shared/models/schedule.model';
import type { Bus } from '../../shared/models/bus.model';

@Component({
  selector: 'app-vendor-dashboard',
  imports: [RouterLink, LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './vendor-dashboard.html',
})
export class VendorDashboardComponent implements OnInit {
  private readonly vendorService = inject(VendorService);
  private readonly scheduleService = inject(ScheduleService);
  readonly auth = inject(AuthService);

  readonly vendor = signal<Vendor | null>(null);
  readonly buses = signal<Bus[]>([]);
  readonly schedules = signal<Schedule[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  // Registration form
  readonly registering = signal(false);
  readonly registerError = signal<string | null>(null);
  readonly vendorName = signal('');
  readonly regEmail = signal('');
  readonly phoneNumber = signal('');
  readonly address = signal('');
  readonly licenseNumber = signal('');

  // Profile edit form (post-approval)
  readonly editingProfile = signal(false);
  readonly profileSaving = signal(false);
  readonly profileError = signal<string | null>(null);
  readonly editVendorName = signal('');
  readonly editPhoneNumber = signal('');
  readonly editAddress = signal('');

  async ngOnInit(): Promise<void> {
    this.regEmail.set(this.auth.email() ?? '');
    try {
      const [v, b, s] = await Promise.all([
        this.vendorService.getMyProfile(),
        this.vendorService.getMyBuses(),
        this.scheduleService.getVendorSchedules(),
      ]);
      this.vendor.set(v);
      this.buses.set(b);
      this.schedules.set(s);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async register(): Promise<void> {
    const n = this.vendorName().trim();
    const e = this.regEmail().trim();
    const p = this.phoneNumber().trim();
    const a = this.address().trim();
    const l = this.licenseNumber().trim();

    if (!n || !e || !p || !a || !l) {
      this.registerError.set('All fields are required.');
      return;
    }

    this.registering.set(true);
    this.registerError.set(null);
    try {
      await this.vendorService.register({ vendorName: n, email: e, phoneNumber: p, address: a, licenseNumber: l });
      this.vendor.set(await this.vendorService.getMyProfile());
    } catch (err: unknown) {
      this.registerError.set((err as Error).message);
    } finally {
      this.registering.set(false);
    }
  }

  startEditProfile(): void {
    const v = this.vendor();
    if (!v) return;
    this.editVendorName.set(v.vendorName);
    this.editPhoneNumber.set(v.phoneNumber);
    this.editAddress.set(v.address ?? '');
    this.profileError.set(null);
    this.editingProfile.set(true);
  }

  cancelEditProfile(): void {
    this.editingProfile.set(false);
  }

  async saveProfile(): Promise<void> {
    const v = this.vendor();
    if (!v) return;
    const vendorName = this.editVendorName().trim();
    const phoneNumber = this.editPhoneNumber().trim();
    const address = this.editAddress().trim();

    if (!vendorName || !phoneNumber || !address) {
      this.profileError.set('All fields are required.');
      return;
    }

    this.profileSaving.set(true);
    this.profileError.set(null);
    try {
      await this.vendorService.updateProfile(v.vendorId, { vendorName, phoneNumber, address });
      this.vendor.set(await this.vendorService.getMyProfile());
      this.editingProfile.set(false);
    } catch (err: unknown) {
      this.profileError.set((err as Error).message);
    } finally {
      this.profileSaving.set(false);
    }
  }
}
