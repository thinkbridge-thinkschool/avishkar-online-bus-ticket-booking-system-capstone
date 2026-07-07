import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { Payment, CreateOrderRequest, CreateOrderResponse, ProcessPaymentRequest } from '../../shared/models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  constructor(private readonly http: HttpClient) {}

  async createOrder(req: CreateOrderRequest): Promise<CreateOrderResponse> {
    return firstValueFrom(
      this.http.post<CreateOrderResponse>('/api/v1/payments/create-order', req)
    );
  }

  async processPayment(cmd: ProcessPaymentRequest): Promise<string> { //  verify & process payment
    const res = await firstValueFrom(
      this.http.post<{ paymentId: string }>('/api/v1/payments', cmd)
    );
    return res.paymentId;
  }

  async getPaymentForBooking(bookingId: string): Promise<Payment> {
    return firstValueFrom(this.http.get<Payment>(`/api/v1/payments/booking/${bookingId}`));
  }

  async getMyPayments(): Promise<Payment[]> {
    return firstValueFrom(this.http.get<Payment[]>('/api/v1/payments/my'));
  }
}
