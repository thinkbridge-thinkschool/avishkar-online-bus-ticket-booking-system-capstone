import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookingService } from '../../core/services/booking.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Booking } from '../../shared/models/booking.model';

@Component({
  selector: 'app-my-bookings',
  imports: [RouterLink, LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './my-bookings.html',
  styleUrl: './my-bookings.css',
})
export class MyBookingsComponent implements OnInit {
  private readonly bookingService = inject(BookingService);

  readonly bookings = signal<Booking[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly filterStatus = signal<string>('all');

  readonly confirmedCount = computed(() =>
    this.bookings().filter(b => b.status === 'Confirmed').length
  );

  readonly totalSpent = computed(() =>
    this.bookings()
      .filter(b => b.status === 'Confirmed')
      .reduce((sum, b) => sum + b.totalAmount, 0)
  );

  readonly filteredBookings = computed(() => {
    const f = this.filterStatus();
    const all = this.bookings();
    if (f === 'all') return all;
    if (f === 'Pending') return all.filter(b => b.status === 'Pending' || b.status === 'PaymentPending');
    return all.filter(b => b.status === f);
  });

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
    return new Date(iso).toLocaleDateString('en-IN', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  formatTime(t: string): string {
    if (!t) return '';
    if (t.includes('T') || t.length > 8) {
      return new Date(t).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true });
    }
    const [h, m] = t.split(':').map(Number);
    const suffix = h >= 12 ? 'PM' : 'AM';
    const hour12 = h % 12 || 12;
    return `${hour12}:${String(m).padStart(2, '0')} ${suffix}`;
  }
}
