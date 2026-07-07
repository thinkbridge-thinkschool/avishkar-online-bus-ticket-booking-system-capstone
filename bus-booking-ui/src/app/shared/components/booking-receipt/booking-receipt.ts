import { Component, computed, input } from '@angular/core';
import type { Booking } from '../../models/booking.model';

@Component({
  selector: 'app-booking-receipt',
  imports: [],
  templateUrl: './booking-receipt.html',
  styleUrl: './booking-receipt.css',
})
export class BookingReceiptComponent {
  readonly booking = input.required<Booking>();

  readonly isConfirmed = computed(() => this.booking().status === 'Confirmed');

  formatTime(t: string | undefined): string {
    if (!t) return '';
    const [h, m] = t.split(':').map(Number);
    const suffix = h >= 12 ? 'PM' : 'AM';
    const hour12 = h % 12 || 12;
    return `${hour12}:${String(m).padStart(2, '0')} ${suffix}`;
  }

  formatDate(d: string | undefined): string {
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

  duration(dep: string | undefined, arr: string | undefined): string {
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
