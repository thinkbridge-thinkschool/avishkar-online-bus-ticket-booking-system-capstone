import { Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookingService } from '../../core/services/booking.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import { BookingReceiptComponent } from '../../shared/components/booking-receipt/booking-receipt';
import type { Booking } from '../../shared/models/booking.model';

@Component({
  selector: 'app-booking-detail',
  imports: [RouterLink, LoadingSpinnerComponent, StatusBadgeComponent, BookingReceiptComponent],
  templateUrl: './booking-detail.html',
  styleUrl: './booking-detail.css',
})
export class BookingDetailComponent implements OnInit {
  readonly id = input.required<string>();
  private readonly bookingService = inject(BookingService);
  private readonly confirmDialog = inject(ConfirmDialogService);

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
    const confirmed = await this.confirmDialog.confirm({
      title: 'Confirm Cancellation',
      message: 'Are you sure you want to cancel this booking?',
      confirmText: 'Cancel Booking',
      danger: true,
    });
    if (!confirmed) return;
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

  printReceipt(): void {
    window.print();
  }

  formatDateTime(iso: string): string {
    if (!iso) return '';
    return new Date(iso).toLocaleString('en-IN', {
      day: 'numeric', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }
}
