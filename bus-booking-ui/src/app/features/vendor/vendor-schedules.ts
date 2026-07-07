import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ScheduleService } from '../../core/services/schedule.service';
import { VendorService } from '../../core/services/vendor.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
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
  private readonly confirmDialog = inject(ConfirmDialogService);

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
      const { routeId, busId, departureTime, arrivalTime, pricePerSeat } = this.form.getRawValue();
      await this.scheduleService.createSchedule({
        routeId,
        busId,
        travelDate: dateOnlyOf(departureTime),
        departureTime: timeOnlyOf(departureTime),
        arrivalTime: timeOnlyOf(arrivalTime),
        basePrice: pricePerSeat,
      });
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
    const confirmed = await this.confirmDialog.confirm({
      title: 'Confirm Delete',
      message: 'Are you sure you want to delete this schedule?',
      confirmText: 'Delete',
      danger: true,
    });
    if (!confirmed) return;
    try {
      await this.scheduleService.deleteSchedule(id);
      this.schedules.update(list => list.filter(s => s.scheduleId !== id));
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    }
  }

  // departureTime/arrivalTime are bare "HH:mm:ss" (.NET TimeOnly) — combine with the
  // schedule's own travelDate to get a datetime new Date() can actually parse.
  formatDT(travelDate: string | undefined, time: string | undefined): string {
    if (!travelDate || !time) return '—';
    const dt = new Date(`${travelDate}T${time}`);
    if (isNaN(dt.getTime())) return '—';
    return dt.toLocaleString('en-IN', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', hour12: true });
  }
}

// <input type="datetime-local"> values are "yyyy-MM-ddTHH:mm" — split into the
// separate DateOnly/TimeOnly fields the schedule API expects.
function dateOnlyOf(datetimeLocal: string): string {
  return datetimeLocal.split('T')[0];
}

function timeOnlyOf(datetimeLocal: string): string {
  const time = datetimeLocal.split('T')[1] ?? '00:00';
  return time.length === 5 ? `${time}:00` : time;
}
