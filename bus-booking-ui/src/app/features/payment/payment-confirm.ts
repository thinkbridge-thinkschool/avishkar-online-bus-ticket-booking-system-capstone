import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BookingService } from '../../core/services/booking.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { Booking } from '../../shared/models/booking.model';

@Component({
  selector: 'app-payment-confirm',
  imports: [RouterLink, LoadingSpinnerComponent],
  templateUrl: './payment-confirm.html',
  styleUrl: './payment-confirm.css',
})
export class PaymentConfirmComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly bookingService = inject(BookingService);

  readonly booking = signal<Booking | null>(null);
  readonly loading = signal(true);

  readonly source = signal('');
  readonly destination = signal('');
  readonly busName = signal('');
  readonly busNumber = signal('');
  readonly departureTime = signal('');
  readonly arrivalTime = signal('');
  readonly travelDate = signal('');

  readonly isConfirmed = computed(() => this.booking()?.status === 'Confirmed');

  async ngOnInit(): Promise<void> {
    const qp = this.route.snapshot.queryParams;
    this.source.set(qp['source'] ?? '');
    this.destination.set(qp['destination'] ?? '');
    this.busName.set(qp['busName'] ?? '');
    this.busNumber.set(qp['busNumber'] ?? '');
    this.departureTime.set(qp['departureTime'] ?? '');
    this.arrivalTime.set(qp['arrivalTime'] ?? '');
    this.travelDate.set(qp['travelDate'] ?? '');

    const bookingId = qp['bookingId'];
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

  formatTime(t: string): string {
    if (!t) return '';
    const [h, m] = t.split(':').map(Number);
    const suffix = h >= 12 ? 'PM' : 'AM';
    const hour12 = h % 12 || 12;
    return `${hour12}:${String(m).padStart(2, '0')} ${suffix}`;
  }

  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d + 'T00:00:00').toLocaleDateString('en-IN', {
      weekday: 'short', day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  formatDateTime(dt: string): string {
    if (!dt) return '';
    return new Date(dt).toLocaleString('en-IN', {
      day: 'numeric', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }

  duration(dep: string, arr: string): string {
    if (!dep || !arr) return '';
    const [dh, dm] = dep.split(':').map(Number);
    const [ah, am] = arr.split(':').map(Number);
    let depMins = dh * 60 + dm;
    let arrMins = ah * 60 + am;
    if (arrMins <= depMins) arrMins += 24 * 60;
    const diff = arrMins - depMins;
    const h = Math.floor(diff / 60);
    const m = diff % 60;
    return m > 0 ? `${h}h ${m}m` : `${h}h`;
  }
}
