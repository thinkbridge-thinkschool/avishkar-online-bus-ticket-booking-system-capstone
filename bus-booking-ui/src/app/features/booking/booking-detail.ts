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
  styleUrl: './booking-detail.css',
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

  copyBookingId(): void {
    const id = this.booking()?.bookingId ?? '';
    if (id) navigator.clipboard.writeText(id).catch(() => {});
  }

  formatDateTime(iso: string): string {
    if (!iso) return '';
    return new Date(iso).toLocaleString('en-IN', {
      day: 'numeric', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }

  formatDate(iso: string): string {
    if (!iso) return '';
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
