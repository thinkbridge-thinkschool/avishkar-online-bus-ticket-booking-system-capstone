import { Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookingService } from '../../core/services/booking.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Booking } from '../../shared/models/booking.model';

@Component({
  selector: 'app-booking-detail',
  imports: [RouterLink, LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './booking-detail.html',
})
export class BookingDetailComponent implements OnInit {
  readonly id = input.required<string>();
  private readonly bookingService = inject(BookingService);

  readonly booking = signal<Booking | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly cancelling = signal(false);

  async ngOnInit(): Promise<void> {
    try {
      this.booking.set(await this.bookingService.getById(this.id()));
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async cancel(): Promise<void> {
    if (!confirm('Are you sure you want to cancel this booking?')) return;
    this.cancelling.set(true);
    try {
      await this.bookingService.cancelBooking(this.id());
      this.booking.update(b => b ? { ...b, status: 'Cancelled' } : b);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.cancelling.set(false);
    }
  }

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleString('en-IN');
  }
}
