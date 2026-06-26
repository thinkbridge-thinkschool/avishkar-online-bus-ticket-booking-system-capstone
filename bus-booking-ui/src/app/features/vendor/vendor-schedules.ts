import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ScheduleService } from '../../core/services/schedule.service';
import { VendorService } from '../../core/services/vendor.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Schedule } from '../../shared/models/schedule.model';
import type { Bus } from '../../shared/models/bus.model';
import type { BusRoute } from '../../shared/models/route.model';
import { RouteService } from '../../core/services/route.service';

@Component({
  selector: 'app-vendor-schedules',
  imports: [RouterLink, ReactiveFormsModule, LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './vendor-schedules.html',
})
export class VendorSchedulesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly scheduleService = inject(ScheduleService);
  private readonly vendorService = inject(VendorService);
  private readonly routeService = inject(RouteService);

  readonly schedules = signal<Schedule[]>([]);
  readonly buses = signal<Bus[]>([]);
  readonly routes = signal<BusRoute[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly showForm = signal(false);

  readonly form = this.fb.nonNullable.group({
    routeId: ['', Validators.required],
    busId: ['', Validators.required],
    departureTime: ['', Validators.required],
    arrivalTime: ['', Validators.required],
    pricePerSeat: [500, [Validators.required, Validators.min(1)]],
  });

  async ngOnInit(): Promise<void> {
    try {
      const [s, b, r] = await Promise.all([
        this.scheduleService.getVendorSchedules(),
        this.vendorService.getMyBuses(),
        this.routeService.getRoutes(),
      ]);
      this.schedules.set(s);
      this.buses.set(b);
      this.routes.set(r);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async addSchedule(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    try {
      await this.scheduleService.createSchedule(this.form.getRawValue());
      this.schedules.set(await this.scheduleService.getVendorSchedules());
      this.showForm.set(false);
      this.form.reset();
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.saving.set(false);
    }
  }

  async deleteSchedule(id: string): Promise<void> {
    if (!confirm('Delete this schedule?')) return;
    try {
      await this.scheduleService.deleteSchedule(id);
      this.schedules.update(list => list.filter(s => s.scheduleId !== id));
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    }
  }

  formatDT(iso: string): string {
    return new Date(iso).toLocaleString('en-IN', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', hour12: true });
  }
}
