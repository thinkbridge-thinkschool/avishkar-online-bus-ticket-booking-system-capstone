import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { Booking, CreateBookingRequest } from '../../shared/models/booking.model';

@Injectable({ providedIn: 'root' })
export class BookingService {
  constructor(private readonly http: HttpClient) {}

  async createBooking(cmd: CreateBookingRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>('/api/v1/bookings', cmd));
  }

  async getMyBookings(): Promise<Booking[]> {
    return firstValueFrom(this.http.get<Booking[]>('/api/v1/bookings/my'));
  }

  async getById(bookingId: string): Promise<Booking> {
    return firstValueFrom(this.http.get<Booking>(`/api/v1/bookings/${bookingId}`));
  }

  async cancelBooking(bookingId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/v1/bookings/${bookingId}/cancel`, {}));
  }
}
