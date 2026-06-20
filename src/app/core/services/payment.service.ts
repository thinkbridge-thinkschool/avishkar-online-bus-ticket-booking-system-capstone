import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { Payment, ProcessPaymentRequest } from '../../shared/models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  constructor(private readonly http: HttpClient) {}

  async processPayment(cmd: ProcessPaymentRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>('/api/v1/payments', cmd));
  }

  async getPaymentForBooking(bookingId: string): Promise<Payment> {
    return firstValueFrom(this.http.get<Payment>(`/api/v1/payments/booking/${bookingId}`));
  }

  async getMyPayments(): Promise<Payment[]> {
    return firstValueFrom(this.http.get<Payment[]>('/api/v1/payments/my'));
  }
}
