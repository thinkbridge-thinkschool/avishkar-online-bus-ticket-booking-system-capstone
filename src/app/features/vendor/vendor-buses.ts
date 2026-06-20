import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';

import { VendorService } from '../../core/services/vendor.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { Bus, BusType } from '../../shared/models/bus.model';

const BUS_TYPES: BusType[] = ['Seater', 'SemiSleeper', 'Sleeper', 'AC', 'NonAC'];

@Component({
  selector: 'app-vendor-buses',
  imports: [ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './vendor-buses.html',
})
export class VendorBusesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly vendorService = inject(VendorService);

  readonly buses = signal<Bus[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly showForm = signal(false);
  readonly busTypes = BUS_TYPES;

  readonly form = this.fb.nonNullable.group({
    busNumber: ['', Validators.required],
    busType: ['Seater' as BusType, Validators.required],
    totalSeats: [40, [Validators.required, Validators.min(10), Validators.max(60)]],
  });

  async ngOnInit(): Promise<void> {
    try {
      this.buses.set(await this.vendorService.getMyBuses());
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async addBus(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    try {
      const { busNumber, busType, totalSeats } = this.form.getRawValue();
      await this.vendorService.addBus({ busNumber, busType, totalSeats });
      this.buses.set(await this.vendorService.getMyBuses());
      this.form.reset({ busNumber: '', busType: 'Seater', totalSeats: 40 });
      this.showForm.set(false);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.saving.set(false);
    }
  }

  async deleteBus(busId: string): Promise<void> {
    if (!confirm('Delete this bus?')) return;
    try {
      await this.vendorService.deleteBus(busId);
      this.buses.update(list => list.filter(b => b.busId !== busId));
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    }
  }
}
