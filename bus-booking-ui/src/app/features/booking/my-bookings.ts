import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookingService } from '../../core/services/booking.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Booking } from '../../shared/models/booking.model';

@Component({
  selector: 'app-my-bookings',
  imports: [RouterLink, LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './my-bookings.html',
})
export class MyBookingsComponent implements OnInit {
  private readonly bookingService = inject(BookingService);

  readonly bookings = signal<Booking[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      this.bookings.set(await this.bookingService.getMyBookings());
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' });
  }
}
