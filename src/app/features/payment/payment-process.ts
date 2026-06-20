import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BookingService } from '../../core/services/booking.service';
import { PaymentService } from '../../core/services/payment.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { Booking } from '../../shared/models/booking.model';
import type { PaymentMethod } from '../../shared/models/payment.model';

@Component({
  selector: 'app-payment-process',
  imports: [ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './payment-process.html',
})
export class PaymentProcessComponent implements OnInit {
  readonly bookingId = input.required<string>();
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly bookingService = inject(BookingService);
  private readonly paymentService = inject(PaymentService);

  readonly booking = signal<Booking | null>(null);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  readonly paymentMethods: { value: PaymentMethod; label: string }[] = [
    { value: 'UPI', label: 'UPI (GPay, PhonePe, Paytm)' },
    { value: 'CreditCard', label: 'Credit Card' },
    { value: 'DebitCard', label: 'Debit Card' },
    { value: 'NetBanking', label: 'Net Banking' },
    { value: 'Wallet', label: 'Wallet' },
  ];

  readonly form = this.fb.nonNullable.group({
    method: ['UPI', Validators.required] as [PaymentMethod | string, typeof Validators.required],
    transactionId: [''],
  });

  async ngOnInit(): Promise<void> {
    try {
      this.booking.set(await this.bookingService.getById(this.bookingId()));
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.error.set(null);
    try {
      const { method, transactionId } = this.form.getRawValue();
      await this.paymentService.processPayment({
        bookingId: this.bookingId(),
        method: method as PaymentMethod,
        transactionId: transactionId || undefined,
      });
      await this.router.navigate(['/payment/confirm'], {
        queryParams: { bookingId: this.bookingId() },
      });
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.submitting.set(false);
    }
  }
}
