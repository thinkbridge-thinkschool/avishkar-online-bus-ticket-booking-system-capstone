import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BookingService } from '../../core/services/booking.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { BookingReceiptComponent } from '../../shared/components/booking-receipt/booking-receipt';
import type { Booking } from '../../shared/models/booking.model';

@Component({
  selector: 'app-payment-confirm',
  imports: [RouterLink, LoadingSpinnerComponent, BookingReceiptComponent],
  templateUrl: './payment-confirm.html',
  styleUrl: './payment-confirm.css',
})
export class PaymentConfirmComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly bookingService = inject(BookingService);

  readonly booking = signal<Booking | null>(null);
  readonly loading = signal(true);

  readonly isConfirmed = computed(() => this.booking()?.status === 'Confirmed');

  async ngOnInit(): Promise<void> {
    const bookingId = this.route.snapshot.queryParams['bookingId'];
    if (bookingId) {
      try {
        this.booking.set(await this.bookingService.getById(bookingId));
      } finally {
        this.loading.set(false);
      }
    } else {
      this.loading.set(false);
    }
  }

  printReceipt(): void {
    window.print();
  }

  copyBookingId(): void {
    const id = this.booking()?.bookingId ?? '';
    if (id) navigator.clipboard.writeText(id).catch(() => {});
  }
}
