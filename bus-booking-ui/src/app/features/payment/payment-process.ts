import { Component, NgZone, OnInit, computed, inject, input, signal } from '@angular/core';
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

  // True while the booking can still be paid for; false once it's Confirmed/Cancelled/PaymentFailed
  // (e.g. a stale "Resume Payment" link opened after the reservation already expired).
  readonly canPay = computed(() => {
    const status = this.booking()?.status;
    return status === 'PaymentPending' || status === 'Pending';
  });

  async ngOnInit(): Promise<void> {    // component automatically runs when the page opens. It reads the bookingId from the URL and fetches the booking details from the backend.
    const qp = this.activatedRoute.snapshot.queryParams;
    this.displaySource.set(qp['source'] ?? '');
    this.displayDestination.set(qp['destination'] ?? ''); // store data
    this.displayBusName.set(qp['busName'] ?? '');
    this.displayBusNumber.set(qp['busNumber'] ?? '');
    this.displayDepartureTime.set(qp['departureTime'] ?? '');
    this.displayArrivalTime.set(qp['arrivalTime'] ?? '');
    this.displayTravelDate.set(qp['travelDate'] ?? '');
    this.displayMinPrice.set(qp['minSeatPrice'] ? +qp['minSeatPrice'] : null);
    this.displaySeatCount.set(qp['seatCount'] ? +qp['seatCount'] : 0);

    try {
      const booking = await this.bookingService.getById(this.bookingId());   // calls service to get booking details from backend and store in booking signal.
      this.booking.set(booking);   // Payment page now displays order summary and payment button. If the booking is already paid or cancelled, the page shows a message instead of the payment button.

      // No redirect query params (e.g. resuming a pending payment from My Bookings
      // instead of arriving fresh from seat selection) — fall back to the booking's
      // own trip data so the order summary isn't blank.
      if (!this.displaySource()) this.displaySource.set(booking.fromCityName ?? '');
      if (!this.displayDestination()) this.displayDestination.set(booking.toCityName ?? '');
      if (!this.displayBusName()) this.displayBusName.set(booking.busName ?? '');
      if (!this.displayBusNumber()) this.displayBusNumber.set(booking.busNumber ?? '');
      if (!this.displayDepartureTime()) this.displayDepartureTime.set(booking.departureTime ?? '');
      if (!this.displayArrivalTime()) this.displayArrivalTime.set(booking.arrivalTime ?? '');
      if (!this.displayTravelDate()) this.displayTravelDate.set(booking.travelDate ?? '');
      if (!this.displaySeatCount()) this.displaySeatCount.set(booking.seats.length);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async pay(): Promise<void> {           // when user click pay via razorpay button
    this.paying.set(true);       // changes the button from "Pay via Razorpay" to "Processing Payment..." and disables the button.
    this.error.set(null);
    try {
      const order = await this.paymentService.createOrder({ bookingId: this.bookingId() }); // SERVICE CREATE ORDER CALLS BACKEND /api/v1/payments/create-order which calls Razorpay API to create order and returns orderId, amount, currency, keyId.

      if (order.keyId === 'mock') {    // Demo Payment Popup
        this.mockOrder = order;
        this.paying.set(false);
        this.showMockModal.set(true);
        return;
      }

      if (typeof window.Razorpay === 'undefined') {
        throw new Error('Razorpay checkout could not be loaded. Please refresh the page.');
      }
      this.openRazorpay(order.orderId, order.amountPaise, order.currency, order.keyId);  // This opens the Razorpay checkout popup.
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
      window.location.href = `/payment/confirm?bookingId=${this.bookingId()}`;
    } catch (err: unknown) {
      const httpErr = err as { error?: { message?: string }; message?: string };
      this.error.set(httpErr?.error?.message ?? httpErr?.message ?? 'Payment failed.');
      this.mockConfirming.set(false);
    }
  }

  private openRazorpay(
    orderId: string,
    amountPaise: number,
    currency: string,
    keyId: string,
  ): void {
    const bookingId = this.bookingId();

    const rzp = new window.Razorpay({
      key: keyId,
      amount: amountPaise,
      currency,
      name: 'BusBooking',
      description: `${this.displaySource()} → ${this.displayDestination()}`,
      order_id: orderId,
      handler: (response) => {
        this.paymentService
          .processPayment({   // calls service to verify & process payment
            bookingId,
            razorpayOrderId: response.razorpay_order_id,
            razorpayPaymentId: response.razorpay_payment_id,
            razorpaySignature: response.razorpay_signature,
          })
          .then(() => {
            window.location.href = `/payment/confirm?bookingId=${bookingId}`;
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
