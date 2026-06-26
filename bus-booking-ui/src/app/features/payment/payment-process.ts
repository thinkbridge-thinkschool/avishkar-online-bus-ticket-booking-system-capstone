import { Component, NgZone, OnInit, inject, input, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BookingService } from '../../core/services/booking.service';
import { PaymentService } from '../../core/services/payment.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { Booking } from '../../shared/models/booking.model';
import type { CreateOrderResponse } from '../../shared/models/payment.model';

@Component({
  selector: 'app-payment-process',
  imports: [RouterLink, LoadingSpinnerComponent],
  templateUrl: './payment-process.html',
  styleUrl: './payment-process.css',
})
export class PaymentProcessComponent implements OnInit {
  readonly bookingId = input.required<string>();
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly bookingService = inject(BookingService);
  private readonly paymentService = inject(PaymentService);
  private readonly ngZone = inject(NgZone);

  readonly booking = signal<Booking | null>(null);
  readonly loading = signal(true);
  readonly paying = signal(false);
  readonly error = signal<string | null>(null);
  readonly showMockModal = signal(false);
  readonly mockConfirming = signal(false);
  private mockOrder: CreateOrderResponse | null = null;

  readonly displaySource = signal('');
  readonly displayDestination = signal('');
  readonly displayBusName = signal('');
  readonly displayBusNumber = signal('');
  readonly displayDepartureTime = signal('');
  readonly displayArrivalTime = signal('');
  readonly displayTravelDate = signal('');
  readonly displayMinPrice = signal<number | null>(null);
  readonly displaySeatCount = signal(0);

  async ngOnInit(): Promise<void> {
    const qp = this.activatedRoute.snapshot.queryParams;
    this.displaySource.set(qp['source'] ?? '');
    this.displayDestination.set(qp['destination'] ?? '');
    this.displayBusName.set(qp['busName'] ?? '');
    this.displayBusNumber.set(qp['busNumber'] ?? '');
    this.displayDepartureTime.set(qp['departureTime'] ?? '');
    this.displayArrivalTime.set(qp['arrivalTime'] ?? '');
    this.displayTravelDate.set(qp['travelDate'] ?? '');
    this.displayMinPrice.set(qp['minSeatPrice'] ? +qp['minSeatPrice'] : null);
    this.displaySeatCount.set(qp['seatCount'] ? +qp['seatCount'] : 0);

    try {
      this.booking.set(await this.bookingService.getById(this.bookingId()));
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async pay(): Promise<void> {
    this.paying.set(true);
    this.error.set(null);
    try {
      const order = await this.paymentService.createOrder({ bookingId: this.bookingId() });

      if (order.keyId === 'mock') {
        this.mockOrder = order;
        this.paying.set(false);
        this.showMockModal.set(true);
        return;
      }

      if (typeof window.Razorpay === 'undefined') {
        throw new Error('Razorpay checkout could not be loaded. Please refresh the page.');
      }
      this.openRazorpay(order.orderId, order.amountPaise, order.currency, order.keyId);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
      this.paying.set(false);
    }
  }

  closeMockModal(): void {
    this.showMockModal.set(false);
    this.mockOrder = null;
  }

  async confirmMockPayment(): Promise<void> {
    if (!this.mockOrder) return;
    this.mockConfirming.set(true);
    this.error.set(null);
    try {
      await this.paymentService.processPayment({
        bookingId:          this.bookingId(),
        razorpayOrderId:    this.mockOrder.orderId,
        razorpayPaymentId:  `mock_pay_${this.mockOrder.orderId.slice(-12)}`,
        razorpaySignature:  'mock_sig',
      });
      window.location.href = `/payment/confirm?${this.buildConfirmParams()}`;
    } catch (err: unknown) {
      const httpErr = err as { error?: { message?: string }; message?: string };
      this.error.set(httpErr?.error?.message ?? httpErr?.message ?? 'Payment failed.');
      this.mockConfirming.set(false);
    }
  }

  private buildConfirmParams(): string {
    return new URLSearchParams({
      bookingId:     this.bookingId(),
      source:        this.displaySource(),
      destination:   this.displayDestination(),
      busName:       this.displayBusName(),
      busNumber:     this.displayBusNumber(),
      departureTime: this.displayDepartureTime(),
      arrivalTime:   this.displayArrivalTime(),
      travelDate:    this.displayTravelDate(),
      minSeatPrice:  this.displayMinPrice() !== null ? String(this.displayMinPrice()) : '',
    }).toString();
  }

  private openRazorpay(
    orderId: string,
    amountPaise: number,
    currency: string,
    keyId: string,
  ): void {
    const bookingId = this.bookingId();
    const params = this.buildConfirmParams();

    const rzp = new window.Razorpay({
      key: keyId,
      amount: amountPaise,
      currency,
      name: 'BusBooking',
      description: `${this.displaySource()} → ${this.displayDestination()}`,
      order_id: orderId,
      handler: (response) => {
        this.paymentService
          .processPayment({
            bookingId,
            razorpayOrderId: response.razorpay_order_id,
            razorpayPaymentId: response.razorpay_payment_id,
            razorpaySignature: response.razorpay_signature,
          })
          .then(() => {
            window.location.href = `/payment/confirm?${params}`;
          })
          .catch((err: unknown) => {
            const httpErr = err as { error?: { message?: string }; message?: string };
            const msg = httpErr?.error?.message ?? httpErr?.message ?? 'Payment confirmation failed.';
            this.ngZone.run(() => {
              this.error.set(msg);
              this.paying.set(false);
            });
          });
      },
      modal: {
        ondismiss: () => this.ngZone.run(() => this.paying.set(false)),
      },
      theme: { color: '#667eea' },
    });
    rzp.open();
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
