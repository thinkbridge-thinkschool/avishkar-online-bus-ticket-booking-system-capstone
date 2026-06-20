import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BookingService } from '../../core/services/booking.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Booking } from '../../shared/models/booking.model';

@Component({
  selector: 'app-payment-confirm',
  imports: [RouterLink, LoadingSpinnerComponent, StatusBadgeComponent],
  template: `
    @if (loading()) {
      <app-loading-spinner />
    } @else if (booking(); as b) {
      <div style="max-width:500px;margin:2rem auto;text-align:center">
        <div class="card">
          <span style="font-size:4rem;display:block;margin-bottom:1rem">
            {{ b.status === 'Confirmed' ? '✅' : '⚠️' }}
          </span>
          <h1>{{ b.status === 'Confirmed' ? 'Booking Confirmed!' : 'Payment Processed' }}</h1>
          <app-status-badge [status]="b.status" />
          <p style="margin-top:1rem;color:#6b7280">Booking ID: <strong>{{ b.bookingId }}</strong></p>
          <p>{{ b.fromCityName }} → {{ b.toCityName }}</p>
          <p style="font-size:1.3rem;font-weight:700;color:#1a2b4a">₹{{ b.totalAmount }}</p>
          <div style="display:flex;gap:1rem;justify-content:center;margin-top:1.5rem">
            <a [routerLink]="['/bookings', b.bookingId]" class="btn btn-secondary">View Booking</a>
            <a routerLink="/" class="btn btn-outline">Home</a>
          </div>
        </div>
      </div>
    }
  `,
})
export class PaymentConfirmComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly bookingService = inject(BookingService);

  readonly booking = signal<Booking | null>(null);
  readonly loading = signal(true);

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
}
