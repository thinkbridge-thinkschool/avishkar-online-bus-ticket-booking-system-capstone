import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { VendorService } from '../../core/services/vendor.service';
import { ScheduleService } from '../../core/services/schedule.service';
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

  readonly vendor = signal<Vendor | null>(null);
  readonly buses = signal<Bus[]>([]);
  readonly schedules = signal<Schedule[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
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
}
